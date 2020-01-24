using Autofac;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Messages.Email;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Kyc.Client;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Settings;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.TemplateFormatter;
using Lykke.Service.Tier.Settings;
using Lykke.SettingsReader;
using IKycDocumentsService = Lykke.Service.Kyc.Abstractions.Services.IKycDocumentsService;

namespace Lykke.Service.Tier.Modules
{
    [UsedImplicitly]
    public class ClientsModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ClientsModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterClientAccountClient(_appSettings.CurrentValue.ClientAccountServiceClient);

            builder.Register<IPersonalDataService>(ctx =>
                    new PersonalDataService(new PersonalDataServiceClientSettings
                    {
                        ApiKey = _appSettings.CurrentValue.PersonalDataServiceClient.ApiKey,
                        ServiceUri = _appSettings.CurrentValue.PersonalDataServiceClient.ServiceUri
                    }, ctx.Resolve<ILogFactory>()))
                .SingleInstance();

            builder.RegisterEmailSenderViaAzureQueueMessageProducer(_appSettings.ConnectionString(x => x.TierService.Db.ClientPersonalInfoConnString));
            builder.RegisterTemplateFormatter(_appSettings.CurrentValue.TemplateFormatterServiceClient.ServiceUrl);

            builder.Register(ctx => new KycDocumentsServiceV2Client(
                    _appSettings.CurrentValue.KycServiceClient,
                    ctx.Resolve<ILogFactory>().CreateLog(nameof(KycDocumentsServiceV2Client))))
                .As<IKycDocumentsServiceV2>()
                .SingleInstance();

            builder.Register(ctx => new KycStatusServiceClient(new KycServiceClientSettings
                    {
                        ServiceUri = _appSettings.CurrentValue.KycServiceClient.ServiceUri,
                        ApiKey = _appSettings.CurrentValue.KycServiceClient.ApiKey
                    }, ctx.Resolve<ILogFactory>().CreateLog(nameof(KycStatusServiceClient))))
                .As<IKycStatusService>()
                .SingleInstance();

            builder.Register(ctx => new KycDocumentsServiceClient(new KycServiceClientSettings
                    {
                        ServiceUri = _appSettings.CurrentValue.KycServiceClient.ServiceUri,
                        ApiKey = _appSettings.CurrentValue.KycServiceClient.ApiKey
                    }, ctx.Resolve<ILogFactory>().CreateLog(nameof(KycDocumentsServiceV2Client))))
                .As<IKycDocumentsService>()
                .SingleInstance();

            builder.RegisterRateCalculatorClient(_appSettings.CurrentValue.RateCalculatorServiceClient.ServiceUrl);
        }
    }
}
