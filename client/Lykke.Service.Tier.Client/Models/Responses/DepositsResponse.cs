using System.Collections.Generic;

namespace Lykke.Service.Tier.Client.Models.Responses
{
    public class DepositsResponse
    {
        public IReadOnlyCollection<ClientDepositOperation> Deposits { get; set; }
    }
}
