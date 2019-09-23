using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.Tier.Client.Models.Responses;
using Refit;

namespace Lykke.Service.Tier.Client.Api
{
    /// <summary>
    /// Countries API interface.
    /// </summary>
    [PublicAPI]
    public interface ICountriesApi
    {
        /// <summary>
        /// Checks if the country is a high risk country
        /// </summary>
        /// <param name="countryCode">ISO3 country code</param>
        /// <returns></returns>
        [Get("/api/countries/ishighrisk/{countryCode}")]
        Task<bool> IsHighRiskCountryAsync(string countryCode);

        /// <summary>
        /// Gets risk for the country
        /// </summary>
        /// <param name="countryCode">ISO3 country code</param>
        /// <returns></returns>
        [Get("/api/countries/risk/{countryCode}")]
        Task<CountryRiskResponse> GetCountryRiskAsync(string countryCode);
    }
}
