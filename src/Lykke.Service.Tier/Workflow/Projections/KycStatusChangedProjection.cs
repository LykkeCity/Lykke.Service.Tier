using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Contract.Events;
using Lykke.Service.Tier.Domain.Services;

namespace Lykke.Service.Tier.Workflow.Projections
{
    public class KycStatusChangedProjection
    {
        private readonly ITierUpgradeService _tierUpgradeService;

        public KycStatusChangedProjection(
            ITierUpgradeService tierUpgradeService
            )
        {
            _tierUpgradeService = tierUpgradeService;
        }

        public async Task Handle(KycStatusChangedEvent evt)
        {
            Enum.TryParse(evt.NewStatus, out KycStatus status);

            var upgradeRequests = await _tierUpgradeService.GetByClientAsync(evt.ClientId);
            var request = upgradeRequests
                .OrderBy(x => x.Tier)
                .FirstOrDefault(x => x.KycStatus != KycStatus.Ok);

            var requestStatus = GetTierStatus(status);

            if (request != null && request.KycStatus != requestStatus)
            {
                await _tierUpgradeService.AddAsync(evt.ClientId, request.Tier, requestStatus,
                    nameof(KycStatusChangedProjection));
            }
        }

        private KycStatus GetTierStatus(KycStatus status)
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
