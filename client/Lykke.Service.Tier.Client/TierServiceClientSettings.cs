using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Tier.Client 
{
    /// <summary>
    /// Tier client settings.
    /// </summary>
    public class TierServiceClientSettings 
    {
        /// <summary>Service url.</summary>
        [HttpCheck("api/isalive")]
        public string ServiceUrl {get; set;}
    }
}
