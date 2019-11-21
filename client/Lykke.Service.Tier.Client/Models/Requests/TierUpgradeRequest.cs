using Lykke.Service.Tier.Contract;

namespace Lykke.Service.Tier.Client.Models.Requests
{
    public class TierUpgradeRequest
    {
        public string ClientId { get; set; }
        public TierModel Tier { get; set; }
        public string KycStatus { get; set; }
        public string Changer { get; set; }
    }
}
