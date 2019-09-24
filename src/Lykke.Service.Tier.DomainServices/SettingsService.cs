using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Services;
using Lykke.Service.Tier.Domain.Settings;

namespace Lykke.Service.Tier.DomainServices
{
    public class SettingsService : ISettingsService
    {
        private readonly Dictionary<CountryRisk, string[]> _countriesSettings;
        private readonly Dictionary<CountryRisk, LimitSettings[]> _limitSettings;
        private readonly int[] _pushLimitsSettings;
        private readonly string _defaultAsset;

        public SettingsService(
            Dictionary<CountryRisk, string[]> countriesSettings,
            Dictionary<CountryRisk, LimitSettings[]> limitSettings,
            int[] pushLimitsSettings,
            string defaultAsset
        )
        {
            _countriesSettings = countriesSettings;
            _limitSettings = limitSettings;
            _pushLimitsSettings = pushLimitsSettings;
            _defaultAsset = defaultAsset;
        }

        public bool IsHighRiskCountry(string countryCode)
        {
            return _countriesSettings.ContainsKey(CountryRisk.High) && _countriesSettings[CountryRisk.High]
                       .Contains(countryCode, StringComparer.InvariantCultureIgnoreCase);
        }

        public CountryRisk? GetCountryRisk(string country)
        {
            foreach (var countryItem in _countriesSettings)
            {
                if (countryItem.Value.Contains(country, StringComparer.InvariantCultureIgnoreCase))
                    return countryItem.Key;
            }

            return null;
        }

        public string GetDefaultAsset()
        {
            return _defaultAsset;
        }

        public LimitSettings GetLimit(CountryRisk risk, AccountTier tier)
        {
            return _limitSettings.ContainsKey(risk)
                ? _limitSettings[risk].FirstOrDefault(x => x.Tier == tier)
                : null;
        }

        public bool IsLimitReachedForNotification(double current, double max)
        {
            var currentPercent = current / max * 100;

            bool result = false;

            foreach (var percent in _pushLimitsSettings.OrderBy(x => x))
            {
                if (currentPercent >= percent)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }
    }
}
