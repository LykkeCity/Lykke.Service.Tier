using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Limitations.Client.Events;
using Lykke.Service.Tier.Domain.Settings;

namespace Lykke.Service.Tier.Domain.Services
{
    public interface ILimitsService
    {
        Task<LimitSettings> GetClientLimitSettingsAsync(string clientId, AccountTier tier, string country);
        CountryRisk? GetCountryRisk(string country);
        bool IsLimitReachedForNotification(double current, double max);
        Task SaveDepositOperationAsync(ClientDepositEvent evt);
        Task DeleteDepositOperationAsync(string clientId, string operationId);
        Task<double> GetClientDepositAmountAsync(string clientId, AccountTier tier);
    }
}
