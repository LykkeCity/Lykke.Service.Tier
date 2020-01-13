using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Messages.Email;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Settings;
using Lykke.Service.TemplateFormatter;
using Lykke.Service.TemplateFormatter.Client;
using Lykke.Service.Tier.Client;
using Lykke.Service.Tier.Client.Models;
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

            ILogFactory logFactory = EmptyLogFactory.Instance;

            var settings = new AppSettings();

            config.Bind(settings);

            var container = BuildContainer(settings);

            var kycStatusesStorage = AzureTableStorage<KycEntity>.Create(
                ConstantReloadingManager.From(settings.KycStatusesConnString), "KycStatuses", logFactory);

            var kycDocumentsStorage = AzureTableStorage<KycDocumentEntity>.Create(
                ConstantReloadingManager.From(settings.KycStatusesConnString), "KycDocuments", logFactory);

            var personalDataService = container.Resolve<IPersonalDataService>();
            var clientAccountClient = container.Resolve<IClientAccountClient>();
            var tierClient =container.Resolve<ITierClient>();
            var templateFormatter = container.Resolve<ITemplateFormatter>();
            var emailSender = container.Resolve<IEmailSender>();

            Console.WriteLine("Gettings KYC Ok clients...");
            var sb = new StringBuilder();
            sb.AppendLine("ClientId,Email,Documents,Country,CountryRisk,Tier,TradesBlocked,CashoutBlocked,IsBanned,Limit,Comment");

            await kycStatusesStorage.GetDataByChunksAsync("Ok", entities =>
            {
                var items = entities.ToList();

                var personalDatas = personalDataService.GetAsync(items.Select(x => x.ClientId)).GetAwaiter().GetResult().ToList();

                Console.WriteLine($"Processing {personalDatas.Count} items");

                foreach (var pd in personalDatas)
                {
                    try
                    {
                        var countryRiskTask = tierClient.Countries.GetCountryRiskAsync(pd.CountryFromPOA);
                        var tierInfoTask = tierClient.Tiers.GetClientTierInfoAsync(pd.Id);
                        var docsTask = kycDocumentsStorage.GetDataAsync(pd.Id);
                        var cashOutBlockTask = clientAccountClient.ClientSettings.GetCashOutBlockSettingsAsync(pd.Id);
                        var isBannedTask = clientAccountClient.BannedClients.IsClientBannedAsync(pd.Id);

                        Task.WhenAll(countryRiskTask, tierInfoTask, docsTask, cashOutBlockTask, isBannedTask).GetAwaiter()
                            .GetResult();

                        CountryRiskResponse countryRisk = countryRiskTask.Result;
                        var cashOutBlock = cashOutBlockTask.Result;
                        var isBanned = isBannedTask.Result;
                        var tierInfo = tierInfoTask.Result;

                        List<string> documentTypes = docsTask.Result
                            .Where(x => x.State == "Approved")
                            .Select(x => x.Type)
                            .ToList();

                        AccountTier tier = AccountTier.Beginner;
                        double limit = 0;

                        string documents = string.Join("/", documentTypes);
                        string comment = string.Empty;

                        if (countryRisk.Risk == null)
                        {
                            comment = "restricted country";

                            tier = AccountTier.ProIndividual;
                            limit = 0;
                        }
                        else
                        {
                            var isTier2 = settings.Tier2Emails.ContainsKey(pd.Email.ToLowerInvariant());

                            if (isTier2)
                            {
                                limit = settings.Tier2Emails[pd.Email.ToLowerInvariant()];
                                tier = AccountTier.ProIndividual;
                                comment += "limits according the list; ";
                            }
                            else
                            {
                                switch (countryRisk.Risk)
                                {
                                    case RiskModel.Low:
                                        limit = 15000;
                                        tier = AccountTier.Advanced;
                                        break;
                                    case RiskModel.Mid:
                                        limit = 7500;
                                        tier = AccountTier.Advanced;
                                        break;
                                    case RiskModel.High:
                                        limit = 2000;
                                        tier = AccountTier.ProIndividual;
                                        break;
                                }
                            }
                        }

                        if (limit > 0)
                        {
                            comment += "send email about upgraded tier; ";
                        }

                        sb.AppendLine($"{pd.Id},{pd.Email},{documents},{pd.CountryFromPOA},{countryRisk.Risk},{tier},{cashOutBlock.TradesBlocked},{cashOutBlock.CashOutBlocked},{isBanned},{limit},{comment}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ClientId = {pd.Id}: {ex.Message}");
                    }
                }
            });

            var filename = $"tiers-migration-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
            Console.WriteLine($"Saving results to {filename}...");

            using (var sw = new StreamWriter(filename))
            {
                sw.Write(sb.ToString());
            }

            Console.WriteLine("Done!");
        }

        private static IContainer BuildContainer(AppSettings settings)
        {
            var builder = new ContainerBuilder();

            ILogFactory logFactory = EmptyLogFactory.Instance;

            builder.RegisterInstance(logFactory);

            builder.RegisterInstance(new PersonalDataService(new PersonalDataServiceClientSettings
            {
                ServiceUri = settings.PdServiceUrl,
                ApiKey = settings.PdApiKey
            }, logFactory)).As<IPersonalDataService>();

            builder.RegisterClientAccountClient(settings.ClintAccountServiceUrl);
            builder.RegisterTemplateFormatter(settings.TemplateFormatterUrl);
            builder.RegisterEmailSenderViaAzureQueueMessageProducer(ConstantReloadingManager.From(settings.ClientPersonalInfoConnString));
            return builder.Build();
        }
    }
}
