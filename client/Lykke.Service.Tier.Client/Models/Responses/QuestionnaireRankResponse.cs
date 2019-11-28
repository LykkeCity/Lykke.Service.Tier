using System;

namespace Lykke.Service.Tier.Client.Models.Responses
{
    public class QuestionnaireRankResponse
    {
        public string ClientId { get; set; }
        public double Rank { get; set; }
        public string Changer { get; set; }
        public string Comment { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
