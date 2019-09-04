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
    }
}
