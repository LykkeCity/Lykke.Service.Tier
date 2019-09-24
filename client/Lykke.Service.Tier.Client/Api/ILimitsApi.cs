using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.Tier.Client.Models.Requests;
using Lykke.Service.Tier.Client.Models.Responses;
using Refit;

namespace Lykke.Service.Tier.Client.Api
{
    /// <summary>
    /// Limits API interface.
    /// </summary>
    [PublicAPI]
    public interface ILimitsApi
    {
        /// <summary>
        /// Sets individual client limit
        /// </summary>
        /// <param name="request">limit request for the client</param>
        /// <returns></returns>
        [Post("/api/limits")]
        Task SetLimitAsync([Body]SetLimitRequest request);

        /// <summary>
        /// Gets individual client limit
        /// </summary>
        /// <param name="clientId">limit request for the client</param>
        /// <returns></returns>
        [Get("/api/limits/{clientId}")]
        Task<LimitResponse> GetLimitAsync(string clientId);
    }
}
