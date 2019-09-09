using Lykke.HttpClientGenerator;
using Lykke.Service.Tier.Client.Api;

namespace Lykke.Service.Tier.Client
{
    /// <summary>
    /// Tier API aggregating interface.
    /// </summary>
    public class TierClient : ITierClient
    {
        public ICountriesApi Countries { get; private set; }
        public ITierUpgradeRequestsApi UpgradeRequests { get; private set; }

        public TierClient(IHttpClientGenerator httpClientGenerator)
        {
            Countries = httpClientGenerator.Generate<ICountriesApi>();
            UpgradeRequests = httpClientGenerator.Generate<ITierUpgradeRequestsApi>();
        }
    }
}
