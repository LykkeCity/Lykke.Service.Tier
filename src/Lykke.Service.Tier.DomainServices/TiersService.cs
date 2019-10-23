using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Services;

namespace Lykke.Service.Tier.DomainServices
{
    public class TiersService : ITiersService
    {
        private readonly ILimitsService _limitsService;
        private readonly ISettingsService _settingsService;
        private readonly ITierUpgradeService _tierUpgradeService;

        public TiersService(
            ILimitsService limitsService,
            ISettingsService settingsService,
            ITierUpgradeService tierUpgradeService
            )
        {
            _limitsService = limitsService;
            _settingsService = settingsService;
            _tierUpgradeService = tierUpgradeService;
        }

        public async Task<ClientTierInfo> GetClientTierInfoAsync(string clientId, AccountTier clientTier, string country)
        {
            CurrentTier currentTier = await GetCurrentTierAync(clientId, clientTier, country);
            TierUpgradeRequest upgradeRequet = await GetUpgradeRequestAsync(clientId);
            NextTier nextTier = await GetNextTierAsync(clientId, clientTier, country, upgradeRequet);

            var result = new ClientTierInfo
            {
                CurrentTier = currentTier,
                NextTier = nextTier,
                UpgradeRequest = upgradeRequet
            };

            return result;
        }

        private async Task<CurrentTier> GetCurrentTierAync(string clientId, AccountTier clientTier, string country)
        {
            var currentDepositAmountTask = _limitsService.GetClientDepositAmountAsync(clientId, clientTier);
            var maxLimitTask = _limitsService.GetClientLimitSettingsAsync(clientId, clientTier, country);

            await Task.WhenAll(currentDepositAmountTask, maxLimitTask);

            return new CurrentTier
            {
                Tier = clientTier,
                Asset = _settingsService.GetDefaultAsset(),
                Current = currentDepositAmountTask.Result,
                MaxLimit = maxLimitTask.Result?.MaxLimit ?? 0
            };
        }

        internal async Task<TierUpgradeRequest> GetUpgradeRequestAsync(string clientId)
        {
            IReadOnlyList<ITierUpgradeRequest> tierUpgradeRequests = await _tierUpgradeService.GetByClientAsync(clientId);

            var rejectedRequest = tierUpgradeRequests
                .OrderBy(x => x.Tier)
                .FirstOrDefault(x => x.KycStatus != KycStatus.Ok && x.KycStatus != KycStatus.Pending);

            if (rejectedRequest != null)
            {
                return new TierUpgradeRequest
                {
                    Tier = rejectedRequest.Tier,
                    Status = rejectedRequest.KycStatus.ToString(),
                    SubmitDate = rejectedRequest.Date
                };
            }

            var lastPendingRequest = tierUpgradeRequests
                .Where(x => x.KycStatus != KycStatus.Ok)
                .OrderByDescending(x => x.Tier)
                .FirstOrDefault();

            if (lastPendingRequest != null)
            {
                return new TierUpgradeRequest
                {
                    Tier = lastPendingRequest.Tier,
                    Status = lastPendingRequest.KycStatus.ToString(),
                    SubmitDate = lastPendingRequest.Date
                };
            }

            return null;
        }

        internal async Task<NextTier> GetNextTierAsync(string clientId, AccountTier clientTier, string country, TierUpgradeRequest upgradeRequest)
        {
            AccountTier? nextTier = GetNextTier(clientTier, country, upgradeRequest);

            if (nextTier.HasValue)
            {
                var nextTierLimits = await _limitsService.GetClientLimitSettingsAsync(clientId, nextTier.Value, country);

                if (nextTierLimits != null)
                {
                    return new NextTier
                    {
                        Tier = nextTier.Value,
                        MaxLimit = nextTierLimits.MaxLimit ?? 0,
                        Documents = nextTierLimits.Documents.Select(x => x.ToString()).ToArray(),
                    };
                }
            }

            return null;
        }

        internal AccountTier? GetNextTier(AccountTier clientTier, string country, TierUpgradeRequest upgradeRequest)
        {
            AccountTier tier = clientTier;

            if (upgradeRequest != null)
            {
                switch (upgradeRequest.Status)
                {
                    case nameof(KycStatus.Rejected):
                    case nameof(KycStatus.RestrictedArea):
                        return upgradeRequest.Tier;
                    case nameof(KycStatus.Pending):
                        tier = upgradeRequest.Tier;
                        break;
                }
            }

            if (tier == AccountTier.ProIndividual)
                return null;

            bool isHighRiskCountry = _settingsService.IsHighRiskCountry(country);

            if (isHighRiskCountry)
                return AccountTier.ProIndividual;

            var values = (AccountTier[]) Enum.GetValues(typeof(AccountTier));

            return values[(int) tier + 1];
        }
    }
}
