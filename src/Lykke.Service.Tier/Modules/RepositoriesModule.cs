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

            builder.Register(ctx =>
                new QuestionsRepository(AzureTableStorage<QuestionEntity>.Create(
                    _appSettings.ConnectionString(x => x.TierService.Db.DataConnString),
                    "Questions", ctx.Resolve<ILogFactory>()))
            ).As<IQuestionsRepository>().SingleInstance();

            builder.Register(ctx =>
                new AnswersRepository(AzureTableStorage<AnswerEntity>.Create(
                    _appSettings.ConnectionString(x => x.TierService.Db.DataConnString),
                    "Answers", ctx.Resolve<ILogFactory>()))
            ).As<IAnswersRepository>().SingleInstance();

            builder.Register(ctx =>
                new UserChoicesRepository(AzureTableStorage<UserChoiceEntity>.Create(
                    _appSettings.ConnectionString(x => x.TierService.Db.DataConnString),
                    "QuestionnaireChoices", ctx.Resolve<ILogFactory>()))
            ).As<IUserChoicesRepository>().SingleInstance();

            builder.Register(ctx =>
                new QuestionsRankRepository(AzureTableStorage<QuestionRankEntity>.Create(
                    _appSettings.ConnectionString(x => x.TierService.Db.DataConnString),
                    "QuestionnaireRanks", ctx.Resolve<ILogFactory>()))
            ).As<IQuestionsRankRepository>().SingleInstance();
        }
    }
}
