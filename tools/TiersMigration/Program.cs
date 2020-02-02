using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Messages.Email;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.History.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Kyc.Client;
using Lykke.Service.Operations.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Settings;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.TemplateFormatter;
using Lykke.Service.Tier.AzureRepositories;
using Lykke.Service.Tier.Client;
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

            var depositsStorage = container.Resolve<INoSQLTableStorage<DepositOperationEntity>>();
            var paymentsStorage = container.Resolve<INoSQLTableStorage<PaymentEntity>>();
            var operationsClient = container.Resolve<IOperationsClient>();
            var clientAccountClient = container.Resolve<IClientAccountClient>();

            Console.WriteLine("Getting client deposits...");
            var sb = new StringBuilder();
            sb.AppendLine("ClientId,OperationId,OperationType,Date,Amount,Base Amount,Comment");

            var deposits = await depositsStorage.GetDataAsync();

            Console.WriteLine($"Processing {deposits.Count} deposits");
            var index = 1;

            foreach (var item in deposits)
            {
                Console.WriteLine($"Processing {index++} of {deposits.Count} deposits");
                var operation = await operationsClient.Get(Guid.Parse(item.OperationId));

                if (operation != null)
                {
                    if (operation.Type == OperationType.Cashout)
                    {
                        await depositsStorage.DeleteIfExistAsync(item.ClientId, item.OperationId);
                        sb.AppendLine($"{item.ClientId},{item.OperationId},{item.OperationType},{item.Date},{item.Amount} {item.Asset},{item.BaseVolume} {item.BaseAsset},{operation.Type} -> deleted");
                    }
                }
                else
                {
                    if (item.OperationType == "CardCashIn")
                    {
                        var payment = await paymentsStorage.GetDataAsync(item.ClientId, item.OperationId);
                        if (payment != null && payment.PaymentSystem == "Swift" && payment.Status != "NotifyDeclined")
                        {
                            await depositsStorage.MergeAsync(item.ClientId, item.OperationId, entity =>
                            {
                                entity.OperationType = "SwiftTransfer";
                                return entity;
                            });
                            sb.AppendLine($"{item.ClientId},{item.OperationId},{item.OperationType},{item.Date},{item.Amount} {item.Asset},{item.BaseVolume} {item.BaseAsset},operationType -> SwiftTransfer");
                        }
                    }
                }
            }

            var filename = $"deposit-fixes-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
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

            builder.RegisterInstance(
                AzureTableStorage<PaymentEntity>.Create(
                    ConstantReloadingManager.From(settings.ClientPersonalInfoConnString), "PaymentTransactions", logFactory)
            ).As<INoSQLTableStorage<PaymentEntity>>();

            builder.RegisterOperationsClient(settings.OperationsServiceUrl);

            return builder.Build();
        }
    }
}
