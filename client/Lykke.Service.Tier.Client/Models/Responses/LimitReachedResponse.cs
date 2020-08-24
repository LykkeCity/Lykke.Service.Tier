using System.Collections.Generic;

namespace Lykke.Service.Tier.Client.Models.Responses
{
    public class LimitReachedResponse
    {
        public IReadOnlyList<string> ClientIds { get; set; }
    }
}
