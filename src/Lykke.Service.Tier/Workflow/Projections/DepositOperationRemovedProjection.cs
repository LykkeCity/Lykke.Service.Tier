using System.Threading.Tasks;
using Lykke.Service.Tier.Domain.Services;
using Lykke.Service.Limitations.Client.Events;

namespace Lykke.Service.Tier.Workflow.Projections
{
    public class DepositOperationRemovedProjection
    {
        private readonly ILimitsService _limitsService;

        public DepositOperationRemovedProjection(
            ILimitsService limitsService
            )
        {
            _limitsService = limitsService;
        }

        public Task Handle(ClientOperationRemovedEvent evt)
        {
            return _limitsService.DeleteDepositOperationAsync(evt.ClientId, evt.OperationId);
        }
    }
}
