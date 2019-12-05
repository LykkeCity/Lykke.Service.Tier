using System;
using Lykke.Service.ClientAccount.Client.Models;

namespace Lykke.Service.Tier.Domain
{
    public class TierUpgradeRequest
    {
        public AccountTier Tier { get; set; }
        public DateTime SubmitDate { get; set; }
        public string Status { get; set; }
        public double Limit { get; set; }
    }
}
