using JetBrains.Annotations;
using Lykke.Service.Tier.Client.Api;

namespace Lykke.Service.Tier.Client
{
    /// <summary>
    /// Tier client interface.
    /// </summary>
    [PublicAPI]
    public interface ITierClient
    {
        /// <summary>Api for countries</summary>
        ICountriesApi Countries { get; }

        /// <summary>Api for tier upgrade requests</summary>
        ITierUpgradeRequestsApi UpgradeRequests { get; }

        /// <summary>Api for tiers</summary>
        ITiersApi Tiers { get; }

        /// <summary>Api for limits</summary>
        ILimitsApi Limits { get; }

        /// <summary>Api for questionnaire</summary>
        IQuestionnaireApi Questionnaire { get; }
    }
}
