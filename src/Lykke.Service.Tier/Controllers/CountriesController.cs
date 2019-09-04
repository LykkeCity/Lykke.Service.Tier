using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.Tier.Client.Api;
using Lykke.Service.Tier.Settings;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Lykke.Service.Tier.Controllers
{
    [Route("api/countries")]
    public class CountriesController : Controller, ICountriesApi
    {
        private readonly CountriesSettings _countriesSettings;

        public CountriesController(
            CountriesSettings countriesSettings
            )
        {
            _countriesSettings = countriesSettings;
        }

        /// <inheritdoc cref="ICountriesApi"/>
        [HttpGet("ishighrisk/{countryCode}")]
        [SwaggerOperation("IsHighRiskCountry")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        public Task<bool> IsHighRiskCountryAsync(string countryCode)
        {
            return Task.FromResult(_countriesSettings.HighRisk.Contains(countryCode, StringComparer.InvariantCultureIgnoreCase));
        }
    }
}
