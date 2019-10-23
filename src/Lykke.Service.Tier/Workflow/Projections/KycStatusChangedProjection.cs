using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Contract.Events;
using Lykke.Service.Tier.Domain.Services;

namespace Lykke.Service.Tier.Workflow.Projections
{
    public class KycStatusChangedProjection
    {
        private readonly ITierUpgradeService _tierUpgradeService;
        private readonly IClientAccountClient _clientAccountClient;

        public KycStatusChangedProjection(
            ITierUpgradeService tierUpgradeService,
            IClientAccountClient clientAccountClient
            )
        {
            _tierUpgradeService = tierUpgradeService;
            _clientAccountClient = clientAccountClient;
        }

        public async Task Handle(KycStatusChangedEvent evt)
        {
            Enum.TryParse(evt.NewStatus, out KycStatus status);

            var upgradeRequests = await _tierUpgradeService.GetByClientAsync(evt.ClientId);

            var apprenticeTier = upgradeRequests.FirstOrDefault(x => x.Tier == AccountTier.Apprentice);

            if (apprenticeTier != null && apprenticeTier.KycStatus != KycStatus.Ok)
            {
                var newStatus = GetApprenticeKycStatus(status);
                await _tierUpgradeService.AddAsync(evt.ClientId, AccountTier.Apprentice, newStatus,
                    nameof(KycStatusChangedProjection));
            }
            else
            {
                if (status == KycStatus.Rejected || status == KycStatus.RestrictedArea)
                {
                    var request = upgradeRequests
                        .OrderBy(x => x.Tier)
                        .FirstOrDefault(x => x.KycStatus != KycStatus.Ok);

                    if (request != null)
                    {
                        await _tierUpgradeService.AddAsync(evt.ClientId, request.Tier, status,
                            nameof(KycStatusChangedProjection));
                    }
                }
            }
        }

        private KycStatus GetApprenticeKycStatus(KycStatus status)
        {
            switch (status)
            {
                case KycStatus.Pending:
                case KycStatus.ReviewDone:
                case KycStatus.JumioInProgress:
                case KycStatus.JumioOk:
                case KycStatus.JumioFailed:
                    return KycStatus.Pending;

                case KycStatus.NeedToFillData:
                    return KycStatus.NeedToFillData;

                case KycStatus.Ok:
                    return KycStatus.Ok;

                case KycStatus.Rejected:
                    return KycStatus.Rejected;
                case KycStatus.RestrictedArea:
                    return KycStatus.RestrictedArea;
                case KycStatus.Complicated:
                    return status;
                default:
                    return status;
            }
        }
    }
}
