using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.ClientAccount.Client.Models.Response.ClientAccountInformation;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Contract.Models;
using Lykke.Service.Tier.Client.Api;
using Lykke.Service.Tier.Client.Models.Responses;
using Lykke.Service.Tier.Contract;
using Lykke.Service.Tier.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Lykke.Service.Tier.Controllers
{
    [Route("api/tiers")]
    public class TiersController : Controller, ITiersApi
    {
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly ITiersService _tiersService;
        private readonly ILimitsService _limitsService;
        private readonly IMapper _mapper;

        public TiersController(
            IClientAccountClient clientAccountClient,
            IPersonalDataService personalDataService,
            ITiersService tiersService,
            ILimitsService limitsService,
            IMapper mapper

            )
        {
            _clientAccountClient = clientAccountClient;
            _personalDataService = personalDataService;
            _tiersService = tiersService;
            _limitsService = limitsService;
            _mapper = mapper;
        }

        /// <inheritdoc cref="ITiersApi"/>
        [HttpGet("client/{clientId}")]
        [SwaggerOperation("GetClientTierInfo")]
        [ProducesResponseType(typeof(TierInfoResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        public async Task<TierInfoResponse> GetClientTierInfoAsync(string clientId)
        {
            var clientTask = _clientAccountClient.ClientAccountInformation.GetByIdAsync(clientId);
            var pdTask = _personalDataService.GetAsync(clientId);

            await Task.WhenAll(clientTask, pdTask);

            ClientInfo client = clientTask.Result;
            IPersonalData pd = pdTask.Result;

            if (pd == null)
                throw new ValidationApiException(HttpStatusCode.NotFound, "Client not found");

            var tierInfo = await _tiersService.GetClientTierInfoAsync(client.Id, client.Tier, pd.CountryFromPOA);

            return _mapper.Map<TierInfoResponse>(tierInfo);
        }

        /// <inheritdoc cref="ITiersApi"/>
        [HttpGet("limit/{clientId}/{tier}")]
        [SwaggerOperation("GetTierLimit")]
        [ProducesResponseType(typeof(TierLimitResponse), (int)HttpStatusCode.OK)]
        public async Task<TierLimitResponse> GetTierLimitAsync(string clientId, AccountTier tier)
        {
            var pd = await _personalDataService.GetAsync(clientId);
            var limit = await _limitsService.GetClientLimitSettingsAsync(clientId, tier, pd.CountryFromPOA);

            return new TierLimitResponse {Limit = limit?.MaxLimit ?? 0};
        }
    }
}
