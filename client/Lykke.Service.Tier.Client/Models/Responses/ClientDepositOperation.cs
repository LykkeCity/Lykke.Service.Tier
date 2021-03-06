using System;

namespace Lykke.Service.Tier.Client.Models.Responses
{
    public class ClientDepositOperation
    {
        public string ClientId { get; set; }
        public string FromClientId { get; set; }
        public string OperationId { get; set; }
        public string Asset { get; set; }
        public double Amount { get; set; }
        public string BaseAsset { get; set; }
        public double BaseVolume { get; set; }
        public string OperationType { get; set; }
        public DateTime Date { get; set; }
    }
}
