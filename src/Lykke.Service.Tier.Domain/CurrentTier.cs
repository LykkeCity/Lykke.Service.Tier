using Lykke.Service.ClientAccount.Client.Models;

namespace Lykke.Service.Tier.Domain
{
    public class CurrentTier
    {
        public AccountTier Tier { get; set; }
        public string Asset { get; set; }
        public double Current { get; set; }
        public double MaxLimit { get; set; }
    }
}
