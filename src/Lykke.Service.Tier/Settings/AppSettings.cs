using JetBrains.Annotations;
using Lykke.Sdk.Settings;

namespace Lykke.Service.Tier.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public TierSettings TierService { get; set; }
    }
}
