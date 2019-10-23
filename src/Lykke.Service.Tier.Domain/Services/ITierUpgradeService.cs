using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Tier.Domain.Services
{
    public interface ITierUpgradeService
    {
        Task AddAsync(string clientId, AccountTier tier, KycStatus status, string changer);
        Task UpdateCountsAsync(string clientId, AccountTier tier, KycStatus? oldStatus, KycStatus newStatus);
        Task<IReadOnlyList<ITierUpgradeRequest>> GetByTierAsync(AccountTier tier);
        Task<ITierUpgradeRequest> GetAsync(string clientId, AccountTier tier);
        Task<IReadOnlyList<ITierUpgradeRequest>> GetByClientAsync(string clientId);
        Task<IReadOnlyList<ITierUpgradeRequest>> GetAllAsync();
        Task<Dictionary<string, int>> GetCountsAsync();
        Task<Dictionary<string, int>> InitCache();
    }
}
