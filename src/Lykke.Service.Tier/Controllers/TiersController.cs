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
using Lykke.Service.Tier.Domain;
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
        private readonly ILimitsService _limitsService;
        private readonly ITierUpgradeService _tierUpgradeService;
        private readonly ISettingsService _settingsService;

        public TiersController(
            IClientAccountClient clientAccountClient,
            IPersonalDataService personalDataService,
            ILimitsService limitsService,
            ITierUpgradeService tierUpgradeService,
            ISettingsService settingsService
            )
        {
            _clientAccountClient = clientAccountClient;
            _personalDataService = personalDataService;
            _limitsService = limitsService;
            _tierUpgradeService = tierUpgradeService;
            _settingsService = settingsService;
        }

        /// <inheritdoc cref="ITiersApi"/>
        [HttpGet("client/{clientId}")]
        [SwaggerOperation("GetClientTierInfo")]
        [ProducesResponseType(typeof(TierInfoResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        public async Task<TierInfoResponse> GetClientTierInfoAsync(string clientId)
        {
            var client = await _clientAccountClient.ClientAccountInformation.GetByIdAsync(clientId);

            if (client == null)
                throw new ValidationApiException(HttpStatusCode.NotFound, "Client not found");

            var pd = await _personalDataService.GetAsync(clientId);
            AccountTier? nextTier = GetNextTier(client.Tier);

            var maxLimitTask = _limitsService.GetClientLimitSettingsAsync(clientId, client.Tier, pd.CountryFromPOA);
            Task<ITierUpgradeRequest> tierUpgradeRequestTask = nextTier.HasValue
                ? _tierUpgradeService.GetAsync(clientId, nextTier.Value)
                : Task.FromResult((ITierUpgradeRequest)null);

            await Task.WhenAll(maxLimitTask, tierUpgradeRequestTask);

            var maxLimit = maxLimitTask.Result;
            var tierUpgradeRequest = tierUpgradeRequestTask.Result;

            TierInfo tierInfo = null;

            if (nextTier.HasValue)
            {
                var nextTierLimits = await _limitsService.GetClientLimitSettingsAsync(clientId, nextTier.Value, pd.CountryFromPOA);

                if (nextTierLimits != null)
                {
                    tierInfo = new TierInfo
                    {
                        Tier = nextTier.Value,
                        MaxLimit = nextTierLimits.MaxLimit,
                        Documents = nextTierLimits.Documents.Select(x => x.ToString()).ToArray(),
                        DocumentsSubmitDate = tierUpgradeRequest?.Date
                    };
                }
            }

            var currentDepositAmount = await _limitsService.GetClientDepositAmountAsync(client.Id, client.Tier);

            return new TierInfoResponse
            {
                Tier = client.Tier,
                Asset =  _settingsService.GetDefaultAsset(),
                Current = currentDepositAmount,
                MaxLimit = maxLimit?.MaxLimit,
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
