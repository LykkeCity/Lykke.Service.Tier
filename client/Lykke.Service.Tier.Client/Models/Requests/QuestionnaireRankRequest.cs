using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Tier.Client.Models.Requests
{
    public class QuestionnaireRankRequest
    {
        [Required]
        public string ClientId { get; set; }
        public double Rank { get; set; }
        [Required]
        public string Changer { get; set; }
        public string Comment { get; set; }
    }
}
