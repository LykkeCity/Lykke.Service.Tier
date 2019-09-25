using System;
using Lykke.Service.ClientAccount.Client.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.Tier.Client.Models.Responses
{
    public class TierInfoResponse
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public AccountTier Tier { get; set; }
        public string Asset { get; set; }
        public double Current { get; set; }
        public double MaxLimit { get; set; }
        public TierInfo NextTier { get; set; }
    }

    public class TierInfo
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public AccountTier Tier { get; set; }
        public double MaxLimit { get; set; }
        public DateTime? DocumentsSubmitDate { get; set; }
        public string[] Documents { get; set; }
    }
}
