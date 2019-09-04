using JetBrains.Annotations;

namespace Lykke.Service.Tier.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class TierSettings
    {
        public DbSettings Db { get; set; }
        public CountriesSettings Countries { get; set; }
        public CqrsSettings Cqrs { get; set; }
        public RedisSettings Redis { get; set; }
    }
}
