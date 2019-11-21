using System.Threading.Tasks;
using Lykke.Service.Limitations.Client.Events;
using Lykke.Service.Tier.Contract;
using Lykke.Service.Tier.Domain.Settings;

namespace Lykke.Service.Tier.Domain.Services
{
    public interface ILimitsService
    {
        Task<LimitSettings> GetClientLimitSettingsAsync(string clientId, AccountTier tier, string country);
        Task SaveDepositOperationAsync(ClientDepositEvent evt);
        Task DeleteDepositOperationAsync(string clientId, string operationId);
        Task<double> GetClientDepositAmountAsync(string clientId, AccountTier tier);
        Task AddLimitAsync(string clientId, double limit, string asset);
        Task<ILimit> GetLimitAsync(string clientId);
    }
}
