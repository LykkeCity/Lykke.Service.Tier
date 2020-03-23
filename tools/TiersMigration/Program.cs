using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Messages.Email;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.ClientAccount.Client.Models.Request.ClientAccount;
using Lykke.Service.History.Client;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Kyc.Client;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Settings;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.TemplateFormatter;
using Lykke.Service.Tier.AzureRepositories;
using Lykke.Service.Tier.Client;
using Lykke.Service.Tier.Client.Models.Requests;
using Lykke.SettingsReader.ReloadingManager;
using Microsoft.Extensions.Configuration;

namespace TiersMigration
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var settings = new AppSettings();

            config.Bind(settings);

            var container = BuildContainer(settings);

            var sb = new StringBuilder();
            sb.AppendLine("ClientId,Email,CountryPOA,KYC status,Tier,Limit,Comment");

            var clientAccountClient = container.Resolve<IClientAccountClient>();
            string continuationToken = null;
            bool done;

            do
            {
                var ids = await clientAccountClient.Clients.GetIdsAsync(continuationToken);
                await ProcessClientsAsync(ids.Ids, container, sb);

                done = string.IsNullOrEmpty(ids.ContinuationToken);
                continuationToken = ids.ContinuationToken;

            } while (!done);

            var filename = $"kor-tier-limit-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
            Console.WriteLine($"Saving results to {filename}...");

            using (var sw = new StreamWriter(filename))
            {
                sw.Write(sb.ToString());
            }

            Console.WriteLine("Done!");
        }

        private static async Task ProcessClientsAsync(IEnumerable<string> clientIds, IContainer container, StringBuilder sb)
        {
            var personalDataService = container.Resolve<IPersonalDataService>();
            var tierClient = container.Resolve<ITierClient>();
            var kycStatusService = container.Resolve<IKycStatusService>();
            var clientAccountClient = container.Resolve<IClientAccountClient>();

            var personalDatas = (await personalDataService.GetAsync(clientIds))
                .Where(x => x.CountryFromPOA == "KOR")
                .ToList();

            if (!personalDatas.Any())
                return;

            Console.WriteLine($"Processing {personalDatas.Count} items");
            int index = 0;

            foreach (var pd in personalDatas.AsParallel())
            {
                try
                {
                    Interlocked.Increment(ref index);
                    Console.WriteLine($"({index} of {personalDatas.Count} chunk). Processing client = {pd.Id}");
                    var kycStatus = await kycStatusService.GetKycStatusAsync(pd.Id);

                    if (kycStatus == KycStatus.Ok)
                    {
                        await Task.WhenAll(
                            tierClient.Limits.SetLimitAsync(new SetLimitRequest {ClientId = pd.Id, Limit = 15000}),
                            clientAccountClient.ClientAccount.ChangeAccountTierAsync(pd.Id,
                                new AccountTierRequest {Tier = AccountTier.Advanced})
                        );

                        sb.AppendLine($"{pd.Id},{pd.Email},{pd.CountryFromPOA ?? "-"},{kycStatus},Advanced,15000,");
                    }


                }
                catch (Exception ex)
                {
                    sb.AppendLine($"{pd.Id},{pd.Email},{pd.CountryFromPOA},-,-,-,{ex.Message}");
                    Console.WriteLine($"ClientId = {pd.Id}: {ex.Message}");
                }
            }
        }

        private static IContainer BuildContainer(AppSettings settings)
        {
            var builder = new ContainerBuilder();

            ILogFactory logFactory = EmptyLogFactory.Instance;

            builder.RegisterInstance(logFactory);
            builder.RegisterInstance(settings);

            builder.RegisterInstance(new PersonalDataService(new PersonalDataServiceClientSettings
            {
                ServiceUri = settings.PdServiceUrl,
                ApiKey = settings.PdApiKey
            }, logFactory)).As<IPersonalDataService>();

            builder.RegisterClientAccountClient(settings.ClintAccountServiceUrl);
            builder.RegisterTemplateFormatter(settings.TemplateFormatterUrl);
            builder.RegisterTierClient(new TierServiceClientSettings{ServiceUrl = settings.TierServiceUrl});
            builder.RegisterEmailSenderViaAzureQueueMessageProducer(ConstantReloadingManager.From(settings.ClientPersonalInfoConnString));
            builder.RegisterHistoryClient(new HistoryServiceClientSettings{ServiceUrl = settings.HistoryServiceUrl});
            builder.RegisterRateCalculatorClient(settings.RateCalculatorServiceUrl);
            builder.Register(ctx => new KycStatusServiceClient(new KycServiceClientSettings
                {
                    ServiceUri = settings.KycServiceUrl,
                    ApiKey = settings.KycApiKey
                }, ctx.Resolve<ILogFactory>().CreateLog(nameof(KycStatusServiceClient))))
                .As<IKycStatusService>()
                .SingleInstance();

            builder.Register(ctx => new KycDocumentsServiceClient(new KycServiceClientSettings
                {
                    ServiceUri = settings.KycServiceUrl,
                    ApiKey = settings.KycApiKey
                }, ctx.Resolve<ILogFactory>().CreateLog(nameof(KycDocumentsServiceClient))))
                .As<Lykke.Service.Kyc.Abstractions.Services.IKycDocumentsService>()
                .SingleInstance();

            builder.RegisterInstance(
                AzureTableStorage<DepositOperationEntity>.Create(
                    ConstantReloadingManager.From(settings.TiersDataConnString), "ClientDeposits", logFactory)
            ).As<INoSQLTableStorage<DepositOperationEntity>>();

            return builder.Build();
        }
    }
}
