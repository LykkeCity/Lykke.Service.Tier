using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Settings;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.TemplateFormatter;
using Lykke.Service.Tier.AzureRepositories;
using Lykke.Service.Tier.Client;
using Lykke.SettingsReader.ReloadingManager;
using Microsoft.Extensions.Configuration;
using JsonConvert = Newtonsoft.Json.JsonConvert;

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
            var rateCalculatorClient = container.Resolve<IRateCalculatorClient>();
            var limitationsClient = new HttpClient();
            limitationsClient.BaseAddress = new Uri(settings.LimitationsServiceUrl);

            Console.WriteLine($"Checking {settings.ClientIds.Count()} clients...");
            var sb = new StringBuilder();
            sb.AppendLine("ClientId,OperationId,OperationType,Date,Amount,Base Amount,Comment");

            var fiatCurrencies = new[] {"USD", "EUR", "CHF", "GBP"};

            foreach (var clientId in settings.ClientIds)
            {
                var limitDataResponse = await limitationsClient.PostAsync($"/api/limitations/GetClientData?clientId={clientId}&period=Month", null);

                if (limitDataResponse.IsSuccessStatusCode)
                {
                    var data = await limitDataResponse.Content.ReadAsStringAsync();
                    var limitations = JsonConvert.DeserializeObject<LimitationsResponse>(data);

                    var allOperations = new List<CashOperationResponse>();
                    allOperations.AddRange(limitations.CashOperations);
                    allOperations.AddRange(limitations.CashTransferOperations);

                    if (allOperations.Any())
                    {
                        foreach (var operation in allOperations)
                        {
                            if (!fiatCurrencies.Contains(operation.Asset, StringComparer.InvariantCultureIgnoreCase))
                                continue;

                            var deposit = await depositsStorage.GetDataAsync(clientId, operation.Id);

                            if (deposit == null)
                            {
                                var baseVolume = operation.Asset == "EUR"
                                    ? (double)operation.Volume
                                    : await rateCalculatorClient.GetAmountInBaseAsync(operation.Asset, (double)operation.Volume,
                                        "EUR");
                                var row =
                                    $"{clientId},{operation.Id},{operation.OperationType},{operation.DateTime},{operation.Volume} {operation.Asset},{baseVolume} EUR,new record";
                                Console.WriteLine(row);
                                sb.AppendLine(row);
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Finished!");
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
