using Lykke.Service.Tier.Contract;

namespace Lykke.Service.Tier.Domain
{
    public class NextTier
    {
        public AccountTier Tier { get; set; }
        public double MaxLimit { get; set; }
        public string[] Documents { get; set; }
    }
}
