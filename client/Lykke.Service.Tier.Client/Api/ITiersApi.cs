using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Tier.Client.Models.Responses;
using Refit;

namespace Lykke.Service.Tier.Client.Api
{
    /// <summary>
    /// Tiers API interface
    /// </summary>
    [PublicAPI]
    public interface ITiersApi
    {
        /// <summary>
        /// Gets tier info for the client
        /// </summary>
        /// <param name="clientId">client Id</param>
        /// <returns></returns>
        [Get("/api/tiers/client/{clientId}")]
        Task<TierInfoResponse> GetClientTierInfoAsync(string clientId);

        /// <summary>
        /// Gets client tier limit
        /// </summary>
        /// <param name="clientId">client Id</param>
        /// <returns></returns>
        [Get("/api/tiers/limit/{clientId}/{tier}")]
        Task<TierLimitResponse> GetTierLimitAsync(string clientId, AccountTier tier);
    }
}
