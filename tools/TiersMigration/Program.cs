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
using Lykke.Service.Tier.Client.Models.Responses;
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
            sb.AppendLine("ClientId,Email,CountryPOA,Country risk,KYC status,currentTier,currentLimit,depositAmount,Comment");

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

            var filename = $"no-poa-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
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

            var personalDatas = (await personalDataService.GetAsync(clientIds))
                .Where(x => x.CountryFromPOA == "KOR" || x.CountryFromPOA == "PRK")
                .ToList();

            Console.WriteLine($"Processing {personalDatas.Count} items");
            int index = 0;

            foreach (var pd in personalDatas.AsParallel())
            {
                try
                {
                    Interlocked.Increment(ref index);
                    Console.WriteLine($"({index} of {personalDatas.Count} chunk). Processing client = {pd.Id}");
                    var tierInfoTask = tierClient.Tiers.GetClientTierInfoAsync(pd.Id);
                    //var countryRiskTask = tierClient.Countries.GetCountryRiskAsync(pd.CountryFromPOA);
                    var kycStatusTask = kycStatusService.GetKycStatusAsync(pd.Id);
                    await Task.WhenAll(tierInfoTask, kycStatusTask);

                    TierInfoResponse tierInfo = tierInfoTask.Result;
                    //CountryRiskResponse countryRisk = countryRiskTask.Result;
                    KycStatus kycStatus = kycStatusTask.Result;

                    sb.AppendLine($"{pd.Id},{pd.Email},{pd.CountryFromPOA ?? "-"},-,{kycStatus},{tierInfo.CurrentTier.Tier},{tierInfo.CurrentTier.MaxLimit},{tierInfo.CurrentTier.Current},-");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"{pd.Id},{pd.Email},{pd.CountryFromPOA},-,-,-,-,-,{ex.Message}");
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

            builder.RegisterInstance(
                AzureTableStorage<DepositOperationEntity>.Create(
                    ConstantReloadingManager.From(settings.TiersDataConnString), "ClientDeposits", logFactory)
            ).As<INoSQLTableStorage<DepositOperationEntity>>();

            return builder.Build();
        }
    }
}
