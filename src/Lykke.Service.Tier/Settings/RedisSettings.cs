using JetBrains.Annotations;

namespace Lykke.Service.Tier.Settings
{
    [UsedImplicitly]
    public class RedisSettings
    {
        public string Configuration { get; set; }
        public string InstanceName { get; set; }
    }
}
