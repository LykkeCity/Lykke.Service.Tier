using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Services;

namespace Lykke.Service.Tier.DomainServices
{
    public class CountriesService: ICountriesService
    {
        private readonly Dictionary<CountryRisk, string[]> _countriesSettings;

        public CountriesService(
            Dictionary<CountryRisk, string[]> countriesSettings
            )
        {
            _countriesSettings = countriesSettings;
        }

        public bool IsHighRiskCountry(string countryCode)
        {
            return _countriesSettings.ContainsKey(CountryRisk.High) && _countriesSettings[CountryRisk.High].Contains(countryCode, StringComparer.InvariantCultureIgnoreCase);
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
    }
}
