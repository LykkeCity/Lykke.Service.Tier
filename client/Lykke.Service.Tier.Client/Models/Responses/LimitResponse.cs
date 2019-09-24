using System;

namespace Lykke.Service.Tier.Client.Models.Responses
{
    public class LimitResponse
    {
        public string ClientId { get; set; }
        public double Limit { get; set; }
        public string Asset { get; set; }
        public DateTime Date { get; set; }
    }
}
