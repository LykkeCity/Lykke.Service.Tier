using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.ClientAccount.Client.Models.Request.ClientAccount;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Contract.Events;

namespace Lykke.Service.Tier.Workflow.Projections
{
    public class KycStatusChangedProjection
    {
        private readonly IClientAccountClient _clientAccountClient;

        public KycStatusChangedProjection(
            IClientAccountClient clientAccountClient
            )
        {
            _clientAccountClient = clientAccountClient;
        }

        public async Task Handle(KycStatusChangedEvent evt)
        {
            if (evt.NewStatus == KycStatus.Ok.ToString())
            {
                var client = await _clientAccountClient.ClientAccountInformation.GetByIdAsync(evt.ClientId);

                if (client.Tier == AccountTier.Beginner)
                {
                    await _clientAccountClient.ClientAccount.ChangeAccountTierAsync(client.Id,
                        new AccountTierRequest { Tier = AccountTier.Apprentice });
                }
            }
        }
    }
}
