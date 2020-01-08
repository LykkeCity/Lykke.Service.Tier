using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Tier.Client.Models.Requests
{
    public class QuestionUpdateRequest
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string Text { get; set; }
        public QuestionTypeModel Type { get; set; }
        public bool Required { get; set; }
        public bool HasOther { get; set; }
        public int Order { get; set; }
    }
}
