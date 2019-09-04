using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Tier.Settings
{
    [UsedImplicitly]
    public class CqrsSettings
    {
        [AmqpCheck]
        public string RabbitConnectionString { get; set; }
    }
}
