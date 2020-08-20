using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models.Request.ClientAccountInformation;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.Tier.Domain.Services;

namespace Lykke.Service.Tier.PeriodicalHandlers
{
    [UsedImplicitly]
    public class LimitReachedHandler : IStartable, IStopable
    {
        private readonly ILimitsService _limitsService;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly TimerTrigger _timerTrigger;
        private readonly ILog _log;

        public LimitReachedHandler(
            ILimitsService limitsService,
            IClientAccountClient clientAccountClient,
            IPersonalDataService personalDataService,
            ILogFactory logFactory
            )
        {
            _limitsService = limitsService;
            _clientAccountClient = clientAccountClient;
            _personalDataService = personalDataService;
            _log = logFactory.CreateLog(this);
            _timerTrigger = new TimerTrigger(nameof(LimitReachedHandler), TimeSpan.FromMinutes(10), logFactory);
            _timerTrigger.Triggered += Execute;
        }

        public void Start()
        {
            _timerTrigger.Start();
        }

        public void Stop()
        {
            _timerTrigger.Stop();
        }

        public void Dispose()
        {
            _timerTrigger.Stop();
            _timerTrigger.Dispose();
        }

        private async Task Execute(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken cancellationToken)
        {
            var limitsReached = await _limitsService.GetAllLimitReachedAsync();

            if (limitsReached.Count == 0)
                return;

            var clientIds = limitsReached.Select(x => x.ClientId).Distinct().ToArray();

            var clients = (await _clientAccountClient.ClientAccountInformation.GetClientsByIdsAsync(
                new ClientIdsRequest {Ids = clientIds })).ToList();

            var personalDatas = (await _personalDataService.GetAsync(clientIds)).ToList();

            foreach (var limit in limitsReached)
            {
                var client = clients.FirstOrDefault(x => x.Id == limit.ClientId);
                var pd = personalDatas.FirstOrDefault(x => x.Id == limit.ClientId);

                if (client == null || pd == null)
                {
                    _log.Warning("Client or personal data not found", context: limit.ClientId);
                    continue;
                }

                var currentLimitSettingsTask = _limitsService.GetClientLimitSettingsAsync(limit.ClientId, client.Tier, pd.CountryFromPOA);
                var checkAmountTask = _limitsService.GetClientDepositAmountAsync(limit.ClientId);

                await Task.WhenAll(currentLimitSettingsTask, checkAmountTask);

                if (currentLimitSettingsTask.Result?.MaxLimit == null)
                    continue;

                var checkAmount = checkAmountTask.Result;

                if (checkAmount < currentLimitSettingsTask.Result.MaxLimit.Value)
                {
                    await _limitsService.RemoveLimitReachedAsync(limit.ClientId);
                    _log.Info("Limit reached removed", context: limit.ClientId);
                }
            }
        }
    }
}
