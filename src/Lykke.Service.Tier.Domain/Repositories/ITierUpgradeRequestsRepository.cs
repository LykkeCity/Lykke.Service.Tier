using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Tier.Domain.Repositories
{
    public interface ITierUpgradeRequestsRepository
    {
        Task AddAsync(string clientId, AccountTier tier, KycStatus status, string comment = null);
        Task<ITierUpgradeRequest> GetAsync(string clientId, AccountTier tier);
        Task<IReadOnlyList<ITierUpgradeRequest>> GetByTierAsync(AccountTier tier);
        Task AddCountAsync(AccountTier tier, int count);
        Task<int> GetCountAsync(AccountTier tier);
        Task<Dictionary<string, int>> GetCountsAsync();
    }
}
