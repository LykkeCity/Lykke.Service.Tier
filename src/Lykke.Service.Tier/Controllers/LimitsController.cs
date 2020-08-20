using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Tier.Client.Api;
using Lykke.Service.Tier.Client.Models.Requests;
using Lykke.Service.Tier.Client.Models.Responses;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Lykke.Service.Tier.Controllers
{
    [Route("api/limits")]
    public class LimitsController : Controller, ILimitsApi
    {
        private readonly ILimitsService _limitsService;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly ISettingsService _settingsService;
        private readonly IMapper _mapper;

        public LimitsController(
            ILimitsService limitsService,
            IClientAccountClient clientAccountClient,
            ISettingsService settingsService,
            IMapper mapper
            )
        {
            _limitsService = limitsService;
            _clientAccountClient = clientAccountClient;
            _settingsService = settingsService;
            _mapper = mapper;
        }

        /// <inheritdoc cref="ILimitsApi"/>
        [HttpPost]
        [SwaggerOperation("SetLimit")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        public async Task SetLimitAsync([FromBody]SetLimitRequest request)
        {
            var client = await _clientAccountClient.ClientAccountInformation.GetByIdAsync(request.ClientId);

            if (client == null)
                throw new ValidationApiException(HttpStatusCode.NotFound, "Client not found");

            await _limitsService.AddLimitAsync(request.ClientId, request.Limit, _settingsService.GetDefaultAsset());
        }

        /// <inheritdoc cref="ILimitsApi"/>
        [HttpGet("{clientId}")]
        [SwaggerOperation("GetLimit")]
        [ProducesResponseType(typeof(LimitResponse), (int)HttpStatusCode.OK)]
        public async Task<LimitResponse> GetLimitAsync(string clientId)
        {
            ILimit limit = await _limitsService.GetLimitAsync(clientId);
            return _mapper.Map<LimitResponse>(limit);
        }

        /// <inheritdoc cref="ILimitsApi"/>
        [HttpGet("reached")]
        [SwaggerOperation("GetLimitReachedAll")]
        [ProducesResponseType(typeof(LimitReachedResponse), (int)HttpStatusCode.OK)]
        public async Task<LimitReachedResponse> GetLimitReachedAllAsync()
        {
            var reached = await _limitsService.GetAllLimitReachedAsync();

            return new LimitReachedResponse {ClientIds = reached.Select(x => x.ClientId).ToList()};
        }

        /// <inheritdoc cref="ILimitsApi"/>
        [HttpGet("reached/{clientId}")]
        [SwaggerOperation("IsLimitReached")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public Task<bool> IsLimitReachedAsync(string clientId)
        {
            return _limitsService.IsLimitReachedAsync(clientId);
        }
    }
}
