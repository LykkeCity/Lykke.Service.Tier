using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Tier.Client.Models
{
    public class ChoiceModel
    {
        [Required]
        public string QuestionId { get; set; }
        public string[] AnswerIds { get; set; }
        public string Other { get; set; }
    }
}
