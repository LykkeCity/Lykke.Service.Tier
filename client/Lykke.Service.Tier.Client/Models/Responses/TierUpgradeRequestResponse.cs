using System;
using Lykke.Service.Tier.Contract;

namespace Lykke.Service.Tier.Client.Models.Responses
{
    public class TierUpgradeRequestResponse
    {
        public string ClientId { get; set; }
        public AccountTier Tier { get; set; }
        public string KycStatus { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
    }
}
