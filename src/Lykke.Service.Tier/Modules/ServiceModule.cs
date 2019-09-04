using System;
using Autofac;
using AzureStorage.Tables;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Messages.Email;
using Lykke.Sdk;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Kyc.Client;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Settings;
using Lykke.Service.TemplateFormatter;
using Lykke.Service.Tier.AzureRepositories;
using Lykke.Service.Tier.Domain.Repositories;
using Lykke.Service.Tier.Domain.Services;
using Lykke.Service.Tier.DomainServices;
using Lykke.Service.Tier.Services;
using Lykke.Service.Tier.Settings;
using Lykke.SettingsReader;
using StackExchange.Redis;

namespace Lykke.Service.Tier.Modules
{
    [UsedImplicitly]
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_appSettings.CurrentValue.TierService.Countries);

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterClientAccountClient(_appSettings.CurrentValue.ClientAccountServiceClient);

            builder.Register<IPersonalDataService>(ctx =>
                    new PersonalDataService(new PersonalDataServiceClientSettings
                    {
                        ApiKey = _appSettings.CurrentValue.PersonalDataServiceClient.ApiKey,
                        ServiceUri = _appSettings.CurrentValue.PersonalDataServiceClient.ServiceUri
                    }, ctx.Resolve<ILogFactory>().CreateLog(nameof(PersonalDataService))))
                .SingleInstance();

            builder.RegisterEmailSenderViaAzureQueueMessageProducer(_appSettings.ConnectionString(x => x.TierService.Db.ClientPersonalInfoConnString));
            builder.RegisterTemplateFormatter(_appSettings.CurrentValue.TemplateFormatterServiceClient.ServiceUrl);

            builder.Register(ctx => new KycDocumentsServiceV2Client(_appSettings.CurrentValue.KycServiceClient, ctx.Resolve<ILogFactory>()))
                .As<IKycDocumentsServiceV2>()
                .SingleInstance();

            builder.RegisterType<TierUpgradeService>()
                .As<ITierUpgradeService>()
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.TierService.Redis.InstanceName))
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();


            builder.Register(c =>
            {
                var options = ConfigurationOptions.Parse(_appSettings.CurrentValue.TierService.Redis.Configuration);
                options.ReconnectRetryPolicy = new ExponentialRetry(3000, 15000);

                var lazy = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(options));
                return lazy.Value;
            }).As<IConnectionMultiplexer>().SingleInstance();

            builder.Register(c => c.Resolve<IConnectionMultiplexer>().GetDatabase())
                .As<IDatabase>();

            builder.Register(ctx =>
                new TierUpgradeRequestsRepository(AzureTableStorage<TierUpgradeRequestEntity>.Create(
                    _appSettings.ConnectionString(x => x.TierService.Db.DataConnString),
                    "TierUpgradeRequests", ctx.Resolve<ILogFactory>()))
            ).As<ITierUpgradeRequestsRepository>().SingleInstance();

            builder.Register(ctx =>
                new AuditLogRepository(AzureTableStorage<AuditLogDataEntity>.Create(
                    _appSettings.ConnectionString(x => x.TierService.Db.ClientPersonalInfoConnString),
                    "AuditLogs", ctx.Resolve<ILogFactory>()))
            ).As<IAuditLogRepository>().SingleInstance();
        }
    }
}
