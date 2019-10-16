using System;
using Autofac;
using JetBrains.Annotations;
using Lykke.Sdk;
using Lykke.Service.Tier.Domain.Services;
using Lykke.Service.Tier.DomainServices;
using Lykke.Service.Tier.Services;
using Lykke.Service.Tier.Settings;
using Lykke.SettingsReader;
using StackExchange.Redis;

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

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.Register(c =>
            {
                var options = ConfigurationOptions.Parse(_appSettings.CurrentValue.TierService.Redis.Configuration);
                options.ReconnectRetryPolicy = new ExponentialRetry(3000, 15000);

                var lazy = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(options));
                return lazy.Value;
            }).As<IConnectionMultiplexer>().SingleInstance();

            builder.Register(c => c.Resolve<IConnectionMultiplexer>().GetDatabase())
                .As<IDatabase>();

            builder.RegisterType<TierUpgradeService>()
                .As<ITierUpgradeService>()
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.TierService.Redis.InstanceName))
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();

            builder.RegisterType<LimitsService>()
                .As<ILimitsService>()
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.TierService.Redis.InstanceName))
                .SingleInstance();

            builder.RegisterType<SettingsService>()
                .As<ISettingsService>()
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.TierService.Countries))
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.TierService.Limits))
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.TierService.PushLimitsReachedAt))
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.TierService.DefaultAsset))
                .SingleInstance();

            builder.RegisterType<TiersService>()
                .As<ITiersService>()
                .SingleInstance();
        }
    }
}
