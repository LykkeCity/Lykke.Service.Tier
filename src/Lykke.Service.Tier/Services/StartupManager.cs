using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Sdk;
using Lykke.Service.Tier.Domain.Services;

namespace Lykke.Service.Tier.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly ICqrsEngine _cqrsEngine;
        private readonly ITierUpgradeService _tierUpgradeService;
        private readonly ILog _log;

        public StartupManager(
            ICqrsEngine cqrsEngine,
            ITierUpgradeService tierUpgradeService,
            ILogFactory logFactory
        )
        {
            _cqrsEngine = cqrsEngine;
            _tierUpgradeService = tierUpgradeService;
            _log = logFactory.CreateLog(this);
        }

        public async Task StartAsync()
        {
            _cqrsEngine.StartSubscribers();
            _cqrsEngine.StartProcesses();

            _log.Info("Caching tier upgrade request counters");

            await _tierUpgradeService.InitCache();

            _log.Info("Tier upgrade request counters cached");
        }
    }
}
