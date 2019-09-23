using System.Threading.Tasks;

namespace Lykke.Service.Tier.Domain.Services
{
    public interface ICountriesService
    {
        bool IsHighRiskCountry(string countryCode);
        CountryRisk? GetCountryRisk(string country);
    }
}
