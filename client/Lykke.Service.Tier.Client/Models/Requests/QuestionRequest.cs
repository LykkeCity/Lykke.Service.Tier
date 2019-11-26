using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Tier.Client.Models.Requests
{
    public class QuestionRequest
    {
        [Required]
        public string Text { get; set; }
        public QuestionTypeModel Type { get; set; }
        public bool Required { get; set; }
        public bool HasOther { get; set; }
        public string[] Answers { get; set; }
    }
}
