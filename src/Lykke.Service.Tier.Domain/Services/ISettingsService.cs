using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Tier.Domain.Settings;

namespace Lykke.Service.Tier.Domain.Services
{
    public interface ISettingsService
    {
        bool IsHighRiskCountry(string countryCode);
        CountryRisk? GetCountryRisk(string country);
        string GetDefaultAsset();
        LimitSettings GetLimit(CountryRisk risk, AccountTier tier);
        bool IsLimitReachedForNotification(double current, double max);
    }
}
