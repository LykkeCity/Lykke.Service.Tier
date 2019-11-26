using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Tier.Client.Models.Requests
{
    public class AnswerUpdateRequest
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string QuestionId { get; set; }
        [Required]
        public string Text { get; set; }
        public int Order { get; set; }
    }
}
