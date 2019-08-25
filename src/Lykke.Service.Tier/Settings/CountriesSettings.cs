using System.Collections.Generic;
using JetBrains.Annotations;

namespace Lykke.Service.Tier.Settings
{
    [UsedImplicitly]
    public class CountriesSettings
    {
        public IReadOnlyList<string> LowRisk { get; set; }
        public IReadOnlyList<string> MidRisk { get; set; }
        public IReadOnlyList<string> HighRisk { get; set; }
    }
}
