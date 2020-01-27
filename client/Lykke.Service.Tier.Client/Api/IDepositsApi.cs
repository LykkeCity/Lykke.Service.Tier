using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.Tier.Client.Models.Responses;
using Refit;

namespace Lykke.Service.Tier.Client.Api
{
    /// <summary>
    /// Deposits API interface.
    /// </summary>
    [PublicAPI]
    public interface IDepositsApi
    {
        /// <summary>
        /// Gets client deposits
        /// </summary>
        /// <param name="clientId">client Id</param>
        /// <returns></returns>
        [Get("/api/deposits/{clientId}")]
        Task<DepositsResponse> GetClientDepositsAsync(string clientId);

        /// <summary>
        /// Deletes client deposit record
        /// </summary>
        /// <param name="clientId">client Id</param>
        /// <param name="operationId">deposit operation Id</param>
        /// <returns></returns>
        [Delete("/api/deposits/{clientId}/{operationId}")]
        Task DeleteClientDepositAsync(string clientId, string operationId);
    }
}
