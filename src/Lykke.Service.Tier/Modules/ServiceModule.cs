using Autofac;
using JetBrains.Annotations;
using Lykke.Service.Tier.Settings;
using Lykke.SettingsReader;

namespace Lykke.Service.Tier.Modules
{
    [UsedImplicitly]
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_appSettings.CurrentValue.TierService.Countries);
        }
    }
}
