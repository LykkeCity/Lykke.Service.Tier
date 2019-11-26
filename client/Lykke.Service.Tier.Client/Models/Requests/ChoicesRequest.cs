using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Tier.Client.Models.Requests
{
    public class ChoicesRequest
    {
        [Required]
        public string ClientId { get; set; }
        public ChoiceModel[] Choices { get; set; }
    }
}
