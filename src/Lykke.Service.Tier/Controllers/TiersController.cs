using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.Tier.Client.Api;
using Lykke.Service.Tier.Client.Models.Responses;
using Lykke.Service.Tier.Domain.Services;
using Lykke.Service.Tier.Domain.Settings;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Lykke.Service.Tier.Controllers
{
    [Route("api/countries")]
    public class TiersController : Controller, ITiersApi
    {
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly ILimitsService _limitsService;

        public TiersController(
            IClientAccountClient clientAccountClient,
            IPersonalDataService personalDataService,
            ILimitsService limitsService
            )
        {
            _clientAccountClient = clientAccountClient;
            _personalDataService = personalDataService;
            _limitsService = limitsService;
        }

        /// <inheritdoc cref="ITiersApi"/>
        [HttpGet("client/{clientId}")]
        [SwaggerOperation("GetClientTierInfo")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        public async Task<TierInfoResponse> GetClientTierInfoAsync(string clientId)
        {
            var client = await _clientAccountClient.ClientAccountInformation.GetByIdAsync(clientId);

            if (client == null)
                throw new ValidationApiException(HttpStatusCode.NotFound, "Client not found");

            var pd = await _personalDataService.GetAsync(clientId);
            LimitSettings maxLimit = await _limitsService.GetClientLimitSettingsAsync(clientId, client.Tier, pd.CountryFromPOA);

            AccountTier? nextTier = GetNextTier(client.Tier);

            TierInfo tierInfo = null;

            if (nextTier.HasValue)
            {
                var nextTierLimits = await _limitsService.GetClientLimitSettingsAsync(clientId, nextTier.Value, pd.CountryFromPOA);

                if (nextTierLimits?.MaxLimit != null)
                {
                    tierInfo = new TierInfo
                    {
                        Tier = nextTier.Value,
                        MaxLimit = nextTierLimits.MaxLimit.Value,
                        Documents = nextTierLimits.Documents.Select(x => x.ToString()).ToArray()
                    };
                }
            }

            var currentDepositAmount = await _limitsService.GetClientDepositAmountAsync(client.Id, client.Tier);

            return new TierInfoResponse
            {
                Tier = client.Tier,
                Asset = "EUR",
                Current = currentDepositAmount,
                MaxLimit = maxLimit?.MaxLimit ?? 0,
                NextTier = tierInfo
            };
        }

        private static AccountTier? GetNextTier(AccountTier tier)
        {
            if (tier == AccountTier.ProIndividual)
                return null;

            var values = (AccountTier[]) Enum.GetValues(typeof(AccountTier));

            return values[((int) tier) + 1];
        }
    }
}
