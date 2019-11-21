using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Tier.Contract;

namespace Lykke.Service.Tier.Domain.Repositories
{
    public interface ITierUpgradeRequestsRepository
    {
        Task AddAsync(string clientId, AccountTier tier, KycStatus status, string comment = null, DateTime? date = null);
        Task<ITierUpgradeRequest> GetAsync(string clientId, AccountTier tier);
        Task<IReadOnlyList<ITierUpgradeRequest>> GetByClientAsync(string clientId);
        Task<IReadOnlyList<ITierUpgradeRequest>> GetByTierAsync(AccountTier tier);
        Task<IReadOnlyList<ITierUpgradeRequest>> GetPendingRequestsAsync();
        Task DeletePendingRequestIndexAsync(string clientId, AccountTier tier);
        Task AddCountAsync(AccountTier tier, int count);
        Task<int> GetCountAsync(AccountTier tier);
        Task<Dictionary<string, int>> GetCountsAsync();
    }
}
