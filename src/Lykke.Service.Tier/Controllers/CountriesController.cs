using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Service.Tier.Client.Api;
using Lykke.Service.Tier.Client.Models;
using Lykke.Service.Tier.Client.Models.Responses;
using Lykke.Service.Tier.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Lykke.Service.Tier.Controllers
{
    [Route("api/countries")]
    public class CountriesController : Controller, ICountriesApi
    {
        private readonly ISettingsService _settingsService;
        private readonly IMapper _mapper;

        public CountriesController(
            ISettingsService settingsService,
            IMapper mapper
            )
        {
            _settingsService = settingsService;
            _mapper = mapper;
        }

        /// <inheritdoc cref="ICountriesApi"/>
        [HttpGet("ishighrisk/{countryCode}")]
        [SwaggerOperation("IsHighRiskCountry")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public Task<bool> IsHighRiskCountryAsync(string countryCode)
        {
            return Task.FromResult(_settingsService.IsHighRiskCountry(countryCode));
        }

        /// <inheritdoc cref="ICountriesApi"/>
        [HttpGet("risk/{countryCode}")]
        [SwaggerOperation("GetCountryRisk")]
        [ProducesResponseType(typeof(CountryRiskResponse), (int)HttpStatusCode.OK)]
        public Task<CountryRiskResponse> GetCountryRiskAsync(string countryCode)
        {
            var result = new CountryRiskResponse
            {
                Risk = _mapper.Map<RiskModel>(_settingsService.GetCountryRisk(countryCode))
            };

            return Task.FromResult(result);
        }
    }
}
