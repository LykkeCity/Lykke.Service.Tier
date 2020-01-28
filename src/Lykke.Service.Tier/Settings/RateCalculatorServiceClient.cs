using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Tier.Settings
{
    [UsedImplicitly]
    public class RateCalculatorServiceClient
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }
}
