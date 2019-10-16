namespace Lykke.Service.Tier.Domain
{
    public class ClientTierInfo
    {
        public CurrentTier CurrentTier { get; set; }
        public NextTier NextTier { get; set; }
        public TierUpgradeRequest UpgradeRequest { get; set; }
    }
}
