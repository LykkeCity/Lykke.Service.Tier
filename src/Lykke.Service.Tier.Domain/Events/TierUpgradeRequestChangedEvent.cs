using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Tier.Contract;

namespace Lykke.Service.Tier.Domain.Events
{
    public class TierUpgradeRequestChangedEvent
    {
        public string ClientId { get; set; }
        public AccountTier Tier { get; set; }
        public KycStatus? OldStatus { get; set; }
        public KycStatus NewStatus { get; set; }
    }
}
