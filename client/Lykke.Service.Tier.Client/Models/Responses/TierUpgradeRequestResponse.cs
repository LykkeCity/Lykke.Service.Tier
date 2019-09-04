using System;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Tier.Client.Models.Responses
{
    public class TierUpgradeRequestResponse
    {
        public string ClientId { get; set; }
        public AccountTier Tier { get; set; }
        public KycStatus KycStatus { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
        public int Count { get; set; }
    }
}
