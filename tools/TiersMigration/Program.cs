using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Messages.Email;
using Lykke.Messages.Email.MessageData;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.ClientAccount.Client.Models.Request.ClientAccount;
using Lykke.Service.History.Client;
using Lykke.Service.History.Contracts.Enums;
using Lykke.Service.History.Contracts.History;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Kyc.Client;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Settings;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.TemplateFormatter;
using Lykke.Service.TemplateFormatter.Client;
using Lykke.Service.Tier.AzureRepositories;
using Lykke.Service.Tier.Client;
using Lykke.Service.Tier.Client.Models;
using Lykke.Service.Tier.Client.Models.Requests;
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

            Console.WriteLine("Gettings KYC Ok clients...");
            var sb = new StringBuilder();
            sb.AppendLine("ClientId,Email,Country,CountryRisk,Tier,Limit,Deposits,ChangeTier,SetLimit,SendEmail,Comment");

            await kycStatusesStorage.GetDataByChunksAsync("Ok", entities =>
            {
                var items = entities.ToList();

                ProcessClientsAsync(items.Select(x => x.ClientId), container, sb).GetAwaiter().GetResult();
            });

            var filename = $"tiers-migration-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
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
            var clientAccountClient = container.Resolve<IClientAccountClient>();
            var kycStatusService = container.Resolve<IKycStatusService>();

            var personalDatas = (await personalDataService.GetAsync(clientIds)).ToList();

            Console.WriteLine($"Processing {personalDatas.Count} items");
            int index = 1;

            foreach (var pd in personalDatas.AsParallel())
            {
                try
                {
                    Console.WriteLine($"{index++} of {personalDatas.Count}. Processing client = {pd.Id}");
                    var tierInfoTask = tierClient.Tiers.GetClientTierInfoAsync(pd.Id);
                    var countryRiskTask = tierClient.Countries.GetCountryRiskAsync(pd.CountryFromPOA);

                    Task.WhenAll(countryRiskTask, tierInfoTask).GetAwaiter().GetResult();

                    TierInfoResponse tierInfo = tierInfoTask.Result;
                    CountryRiskResponse countryRisk = countryRiskTask.Result;

                    if (tierInfo.CurrentTier.Tier != AccountTier.Beginner)
                        continue;

                    AccountTier tier;
                    double limit;
                    string comment = string.Empty;
                    string changeTier = "-";
                    string setLimit = "-";
                    string sendEmail = "-";
                    bool isRestricted = countryRisk.Risk == null;
                    double totalDepositAmount = 0;
                    (tier, limit, comment) = GetTierAndLimit(container, countryRisk.Risk, pd.Email.ToLowerInvariant());

                    try
                    {
                        clientAccountClient.ClientAccount
                            .ChangeAccountTierAsync(pd.Id, new AccountTierRequest {Tier = tier}).GetAwaiter()
                            .GetResult();
                        changeTier = "success";
                    }
                    catch(Exception ex)
                    {
                        changeTier = ex.Message;
                    }

                    // try
                    // {
                    //     tierClient.Limits.SetLimitAsync(new SetLimitRequest {ClientId = pd.Id, Limit = limit})
                    //         .GetAwaiter().GetResult();
                    //     setLimit = "success";
                    // }
                    // catch (Exception ex)
                    // {
                    //     setLimit = ex.Message;
                    // }
                    //
                    // var totalDepositAmount = MigrateDepositsAsync(container, pd.Id).GetAwaiter().GetResult();
                    //
                    // if (totalDepositAmount > Convert.ToDecimal(limit) && !isRestricted)
                    // {
                    //     if (string.IsNullOrEmpty(comment))
                    //         comment = "Deposited amount is bigger than limit!";
                    //     else
                    //         comment += "; Deposited amount is bigger than limit!";
                    //
                    //     try
                    //     {
                    //         kycStatusService.ChangeKycStatusAsync(pd.Id, KycStatus.NeedToFillData,
                    //                 $"TiersMigration script - limit reached ({totalDepositAmount} of {limit} EUR)")
                    //             .GetAwaiter().GetResult();
                    //         comment += "; Kyc status changed to NeedToFillData";
                    //     }
                    //     catch (Exception ex)
                    //     {
                    //         comment += $"; Error changing kyc status to NeedToFillData: {ex.Message}";
                    //     }
                    // }
                    //
                    // if (limit > 0)
                    // {
                    //     try
                    //     {
                    //         SendEmailAsync(container, tierInfo, tier, pd.Email).GetAwaiter().GetResult();
                    //         sendEmail = "success";
                    //     }
                    //     catch (Exception ex)
                    //     {
                    //         sendEmail = ex.Message;
                    //     }
                    // }

                    sb.AppendLine($"{pd.Id},{pd.Email},{pd.CountryFromPOA},{countryRisk.Risk},{tier},{limit},{totalDepositAmount},{changeTier},{setLimit},{sendEmail},{comment}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"{pd.Id},{pd.Email},{pd.CountryFromPOA},-,-,-,-,-,-,-,{ex.Message}");
                    Console.WriteLine($"ClientId = {pd.Id}: {ex.Message}");
                }
            }
        }

        private static (AccountTier tier, double limit, string comment) GetTierAndLimit(IContainer container, RiskModel? countryRisk, string email)
        {
            var settings = container.Resolve<AppSettings>();

            if (countryRisk == null)
            {
                return (AccountTier.ProIndividual, 0, "restricted country");
            }

            var isTier2 = settings.Tier2Emails.ContainsKey(email);

            if (isTier2)
            {
                return (AccountTier.ProIndividual, settings.Tier2Emails[email], "limits according the list");
            }

            double limit = 0;
            AccountTier tier = AccountTier.Beginner;

            switch (countryRisk.Value)
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

            return (tier, limit, string.Empty);
        }

        private static async Task SendEmailAsync(IContainer container, TierInfoResponse tierInfo, AccountTier tier, string email)
        {
            var sb = new StringBuilder();
            var templateFormatter = container.Resolve<ITemplateFormatter>();
            var emailSender = container.Resolve<IEmailSender>();

            if (tierInfo.NextTier != null)
            {
                sb.AppendLine(
                    $"If you wish to increase deposit limit, just upgrade to an {tierInfo.NextTier.Tier} account.");
                sb.AppendLine("<br>Or use Lykke Wallet mobile app (More->Profile->Upgrade)");
            }

            var emailTemplate = await templateFormatter.FormatAsync("TierUpgradedTemplate", null,
                "EN", new
                {
                    Tier = tier.ToString(),
                    Year = DateTime.UtcNow.Year,
                    Amount = $"{tierInfo.CurrentTier.MaxLimit} {tierInfo.CurrentTier.Asset}",
                    UpgradeText = sb.ToString()
                });

            var msgData = new PlainTextData
            {
                Sender = email,
                Subject = emailTemplate.Subject,
                Text = emailTemplate.HtmlBody
            };

            await emailSender.SendEmailAsync(null, email, msgData);
        }

        public static async Task<decimal> MigrateDepositsAsync(IContainer container, string clientId)
        {
            var historyClient = container.Resolve<IHistoryClient>();
            var rateCalculatorClient = container.Resolve<IRateCalculatorClient>();
            var depositsStorage = container.Resolve<INoSQLTableStorage<DepositOperationEntity>>();

            var to = DateTime.UtcNow;
            var from = to.AddMonths(-1);

            var historyItems = (await historyClient.HistoryApi.GetHistoryByWalletAsync(Guid.Parse(clientId),
                            new[] {HistoryType.CashIn}, limit: int.MaxValue,
                            @from: from, to: to))
                .Select(x => (CashinModel)x).ToList();

            decimal result = 0;

            var deposits = new List<DepositOperationEntity>();

            foreach (var item in historyItems)
            {
                decimal amount;

                if (item.AssetId == "EUR")
                {
                    amount = item.Volume;
                }
                else
                {
                    amount = (decimal)rateCalculatorClient
                        .GetAmountInBaseAsync(item.AssetId, (double)item.Volume, "EUR")
                        .GetAwaiter().GetResult();
                }

                if (amount == 0)
                    continue;

                var operationType = new[] {"EUR", "USD", "CHF", "GBP"}.Contains(item.AssetId)
                    ? "CardCashIn"
                    : "CryptoCashIn";

                result += amount;

                deposits.Add(new DepositOperationEntity
                {
                    PartitionKey = item.WalletId.ToString(),
                    RowKey = item.Id.ToString(),
                    ClientId = item.WalletId.ToString(),
                    OperationId = item.Id.ToString(),
                    Asset = item.AssetId,
                    Amount = (double)item.Volume,
                    BaseAsset = "EUR",
                    BaseVolume = (double)amount,
                    OperationType = operationType,
                    Date = item.Timestamp
                });
            }

            await depositsStorage.InsertOrMergeBatchAsync(deposits);

            return result;
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
