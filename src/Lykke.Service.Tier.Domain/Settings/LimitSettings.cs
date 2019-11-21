using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Service.Tier.Contract;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Tier.Domain.Settings
{
    [UsedImplicitly]
    public class LimitSettings
    {
        public AccountTier Tier { get; set; }
        [Optional]
        public double? MaxLimit { get; set; }
        public IReadOnlyList<DocumentType> Documents { get; set; }
    }
}
