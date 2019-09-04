using JetBrains.Annotations;
using Lykke.Sdk.Settings;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Kyc.Client;
using Lykke.Service.PersonalData.Settings;
using Lykke.Service.TemplateFormatter.Client;

namespace Lykke.Service.Tier.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public TierSettings TierService { get; set; }
        public ClientAccountServiceClientSettings ClientAccountServiceClient { get; set; }
        public PersonalDataServiceClientSettings PersonalDataServiceClient { get; set; }
        public TemplateFormatterServiceClientSettings TemplateFormatterServiceClient { get; set; }
        public KycServiceClientSettings KycServiceClient { get; set; }
    }
}
