using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.Tier.Client.Models;
using Lykke.Service.Tier.Client.Models.Requests;
using Lykke.Service.Tier.Client.Models.Responses;
using Refit;

namespace Lykke.Service.Tier.Client.Api
{
    /// <summary>
    /// Tier upgrade requests API interface
    /// </summary>
    [PublicAPI]
    public interface ITierUpgradeRequestsApi
    {
        /// <summary>
        /// Gets upgrade request counters
        /// </summary>
        /// <returns></returns>
        [Get("/api/tierupgraderequests/count")]
        Task<Dictionary<string, int>> GetCountsAsync();

        /// <summary>
        /// Gets client tier upgrade request
        /// </summary>
        /// <returns></returns>
        [Get("/api/tierupgraderequests/{clientId}/{tier}")]
        Task<TierUpgradeRequestResponse> GetAsync(string clientId, TierModel tier);

        /// <summary>
        /// Gets all pending tier upgrade requests
        /// </summary>
        /// <returns></returns>
        [Get("/api/tierupgraderequests/pending")]
        Task<IReadOnlyList<TierUpgradeRequestResponse>> GetPendingRequestsAsync();

        /// <summary>
        /// Gets client tier upgrade requests
        /// </summary>
        /// <returns></returns>
        [Get("/api/tierupgraderequests/client/{clientId}")]
        Task<IReadOnlyList<TierUpgradeRequestResponse>> GetByClientAsync(string clientId);

        /// <summary>
        /// Gets tier upgrade requests
        /// </summary>
        /// <returns></returns>
        [Get("/api/tierupgraderequests/{tier}")]
        Task<IReadOnlyList<TierUpgradeRequestResponse>> GetByTierAsync(TierModel tier);

        /// <summary>
        /// Adds or updates tier upgrade request
        /// </summary>
        /// <returns></returns>
        [Post("/api/tierupgraderequests")]
        Task AddAsync([Body]TierUpgradeRequest request);

    }
}
