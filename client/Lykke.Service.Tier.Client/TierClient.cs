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
        public ITiersApi Tiers { get; set; }
        public ILimitsApi Limits { get; set; }
        public IQuestionnaireApi Questionnaire { get; set; }
        public IDepositsApi Deposits { get; set; }

        public TierClient(IHttpClientGenerator httpClientGenerator)
        {
            Countries = httpClientGenerator.Generate<ICountriesApi>();
            UpgradeRequests = httpClientGenerator.Generate<ITierUpgradeRequestsApi>();
            Tiers = httpClientGenerator.Generate<ITiersApi>();
            Limits = httpClientGenerator.Generate<ILimitsApi>();
            Questionnaire = httpClientGenerator.Generate<IQuestionnaireApi>();
            Deposits = httpClientGenerator.Generate<IDepositsApi>();
        }
    }
}
