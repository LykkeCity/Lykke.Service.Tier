using Autofac;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Service.Tier.AzureRepositories;
using Lykke.Service.Tier.Domain.Repositories;
using Lykke.Service.Tier.Settings;
using Lykke.SettingsReader;

namespace Lykke.Service.Tier.Modules
{
    [UsedImplicitly]
    public class RepositoriesModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public RepositoriesModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(ctx =>
                new LimitsRepository(AzureTableStorage<LimitEntity>.Create(
                    _appSettings.ConnectionString(x => x.TierService.Db.DataConnString),
                    "IndividualLimits", ctx.Resolve<ILogFactory>()))
            ).As<ILimitsRepository>().SingleInstance();

            builder.Register(ctx =>
                new TierUpgradeRequestsRepository(AzureTableStorage<TierUpgradeRequestEntity>.Create(
                    _appSettings.ConnectionString(x => x.TierService.Db.DataConnString),
                    "TierUpgradeRequests", ctx.Resolve<ILogFactory>()),
                    AzureTableStorage<AzureIndex>.Create(
                        _appSettings.ConnectionString(x => x.TierService.Db.DataConnString),
                        "TierUpgradeRequests", ctx.Resolve<ILogFactory>()))
            ).As<ITierUpgradeRequestsRepository>().SingleInstance();

            builder.Register(ctx =>
                new AuditLogRepository(AzureTableStorage<AuditLogDataEntity>.Create(
                    _appSettings.ConnectionString(x => x.TierService.Db.ClientPersonalInfoConnString),
                    "AuditLogs", ctx.Resolve<ILogFactory>()))
            ).As<IAuditLogRepository>().SingleInstance();

            builder.Register(ctx =>
                new ClientDepositsRepository(AzureTableStorage<DepositOperationEntity>.Create(
                    _appSettings.ConnectionString(x => x.TierService.Db.DataConnString),
                    "ClientDeposits", ctx.Resolve<ILogFactory>()))
            ).As<IClientDepositsRepository>().SingleInstance();
        }
    }
}
