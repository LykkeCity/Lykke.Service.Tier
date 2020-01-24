using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.MatchingEngine.Connector.Models.Events.Common;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Tier.Contract;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Workflow.Events;

namespace Lykke.Service.Tier.RabbitSubscribers
{
    public sealed class CashTransferSubscriber : IStartable, IDisposable
    {
        private readonly ILogFactory _logFactory;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly ICurrencyConverter _currencyConverter;
        private readonly ICqrsEngine _cqrsEngine;
        private RabbitMqSubscriber<CashTransferEvent> _subscriber;
        private readonly ILog _log;

        public CashTransferSubscriber(
            string connectionString,
            string exchangeName,
            IClientAccountClient clientAccountClient,
            ICurrencyConverter currencyConverter,
            ICqrsEngine cqrsEngine,
            ILogFactory logFactory
        )
        {
            _logFactory = logFactory;
            _log = _logFactory.CreateLog(this);
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _clientAccountClient = clientAccountClient;
            _currencyConverter = currencyConverter;
            _cqrsEngine = cqrsEngine;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, $"tiers-{nameof(CashTransferSubscriber)}")
                .MakeDurable()
                .UseRoutingKey(((int)MessageType.CashTransfer).ToString());

            _subscriber = new RabbitMqSubscriber<CashTransferEvent>(_logFactory,
                    settings,
                    new ResilientErrorHandlingStrategy(_logFactory, settings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_logFactory, settings)))
                .SetMessageDeserializer(new ProtobufMessageDeserializer<CashTransferEvent>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        private async Task ProcessMessageAsync(CashTransferEvent item)
        {
            var transfer = item.CashTransfer;
            Console.WriteLine(item.ToJson());
            if (await IsTransferBetweenClientWalletsAsync(transfer.FromWalletId, transfer.ToWalletId))
            {
                _log.Info("Skip transfer between client wallets", context: item.ToJson());
                return;
            }

            double volume = Convert.ToDouble(transfer.Volume);
            (double convertedVolume, string assetId) = await _currencyConverter.ConvertAsync(transfer.AssetId, volume);

            if (convertedVolume == 0)
                return;

            string operationType = GetOperationType(item.CashTransfer);

            _cqrsEngine.PublishEvent(new ClientDepositedEvent
            {
                ClientId = transfer.ToWalletId,
                FromClientId = transfer.FromWalletId,
                OperationId = item.Header.MessageId ?? item.Header.RequestId,
                Asset = transfer.AssetId,
                Amount = volume,
                BaseAsset = assetId,
                BaseVolume = convertedVolume,
                OperationType = operationType,
                Timestamp = item.Header.Timestamp
            }, TierBoundedContext.Name);
        }

        private string GetOperationType(CashTransfer cashTransfer)
        {
            var fiatCurrencies = new[] {"USD", "EUR", "CHF", "GBP"};

            if (fiatCurrencies.Contains(cashTransfer.AssetId))
            {
                return cashTransfer.Fees != null ? "CardCashIn" : "SwiftTransfer";
            }

            return "CryptoCashIn";
        }

        private async Task<bool> IsTransferBetweenClientWalletsAsync(string fromWalletId, string toWalletId)
        {
            var fromClientIdTask = _clientAccountClient.Wallets.GetClientIdByWalletAsync(fromWalletId);
            var toClientIdTask = _clientAccountClient.Wallets.GetClientIdByWalletAsync(toWalletId);

            await Task.WhenAll(fromClientIdTask, toClientIdTask);

            return fromClientIdTask.Result.ClientId == toClientIdTask.Result.ClientId;
        }

        #region Dispose
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _subscriber?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CashTransferSubscriber()
        {
            Dispose(false);
        }
        #endregion
    }
}
