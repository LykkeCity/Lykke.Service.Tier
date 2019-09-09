using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Tier.Domain.Deposits;

namespace Lykke.Service.Tier.Domain.Repositories
{
    public interface IClientDepositsRepository
    {
        Task AddAsync(IDepositOperation operation);
        Task DeleteAsync(string clientId, string operationId);
        Task<IEnumerable<IDepositOperation>> GetDepositsAsync(string clientId);
    }
}
