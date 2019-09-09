using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.Tier.Client.Api;
using Lykke.Service.Tier.Domain;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Lykke.Service.Tier.Controllers
{
    [Route("api/countries")]
    public class CountriesController : Controller, ICountriesApi
    {
        private readonly Dictionary<CountryRisk, string[]> _countriesSettings;

        public CountriesController(
            Dictionary<CountryRisk, string[]> countriesSettings
            )
        {
            _countriesSettings = countriesSettings;
        }

        /// <inheritdoc cref="ICountriesApi"/>
        [HttpGet("ishighrisk/{countryCode}")]
        [SwaggerOperation("IsHighRiskCountry")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public Task<bool> IsHighRiskCountryAsync(string countryCode)
        {
            return Task.FromResult(_countriesSettings.ContainsKey(CountryRisk.High) && _countriesSettings[CountryRisk.High].Contains(countryCode, StringComparer.InvariantCultureIgnoreCase));
        }
    }
}
