using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.Tier.Client.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum QuestionTypeModel
    {
        Single,
        Multiple,
        Text
    }
}
