using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Settings;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Tier.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class TierSettings
    {
        public DbSettings Db { get; set; }
        public Dictionary<CountryRisk, string[]> Countries { get; set; }
        public Dictionary<CountryRisk, LimitSettings[]> Limits { get; set; }
        public int[] PushLimitsReachedAt { get; set; }
        public CqrsSettings Cqrs { get; set; }
        public RedisSettings Redis { get; set; }
        public string DefaultAsset { get; set; }
        public RabbitMqSettings Rabbit { get; set; }
        public IReadOnlyList<string> DepositCurrencies { get; set; }
        [Optional] public IReadOnlyList<string> SkipClientIds { get; set; } = Array.Empty<string>();
    }
}
