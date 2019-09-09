using Lykke.Service.ClientAccount.Client.Models;

namespace Lykke.Service.Tier.Client.Models.Responses
{
    public class TierInfoResponse
    {
        public AccountTier Tier { get; set; }
        public string Asset { get; set; }
        public double Current { get; set; }
        public double MaxLimit { get; set; }
        public TierInfo NextTier { get; set; }
    }

    public class TierInfo
    {
        public AccountTier Tier { get; set; }
        public double MaxLimit { get; set; }
        public string[] Documents { get; set; }
    }
}
