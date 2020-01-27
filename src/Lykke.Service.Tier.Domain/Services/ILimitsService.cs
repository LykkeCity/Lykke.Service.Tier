using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Tier.Domain.Deposits;
using Lykke.Service.Tier.Domain.Settings;

namespace Lykke.Service.Tier.Domain.Services
{
    public interface ILimitsService
    {
        Task<LimitSettings> GetClientLimitSettingsAsync(string clientId, AccountTier tier, string country);
        Task SaveDepositOperationAsync(IDepositOperation deposit);
        Task DeleteDepositOperationAsync(string clientId, string operationId);
        Task<double> GetClientDepositAmountAsync(string clientId, AccountTier tier);
        Task AddLimitAsync(string clientId, double limit, string asset);
        Task<ILimit> GetLimitAsync(string clientId);
        Task<IEnumerable<IDepositOperation>> GetClientDepositsAsync(string clientId);
    }
}
