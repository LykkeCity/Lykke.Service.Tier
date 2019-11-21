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

            if (status == KycStatus.Rejected || status == KycStatus.RestrictedArea)
            {
                var request = upgradeRequests
                    .OrderBy(x => x.Tier)
                    .FirstOrDefault(x => x.KycStatus != KycStatus.Ok);

                if (request != null && request.KycStatus != status)
                {
                    await _tierUpgradeService.AddAsync(evt.ClientId, request.Tier, status,
                        nameof(KycStatusChangedProjection));
                }
            }
        }
    }
}
