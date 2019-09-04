using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace Lykke.Service.Tier.Client.Models.Requests
{
    public class TierUpgradeRequest
    {
        public string ClientId { get; set; }
        public TierModel Tier { get; set; }
        public KycStatus KycStatus { get; set; }
        public string Changer { get; set; }
        public string Comment { get; set; }
    }
}
