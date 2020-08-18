using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Tier.Domain.Repositories
{
    public interface ILimitsReachedRepository
    {
        Task AddAsync(string clientId, double amount, double maxAmount, string asset);
        Task RemoveAsync(string clientId);
        Task<ILimitReached> GetAsync(string clientId);
        Task<IReadOnlyList<ILimitReached>> GetAllAsync();
    }
}
