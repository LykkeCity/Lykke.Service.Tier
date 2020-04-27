using System;
using System.Collections.Generic;
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
using Lykke.Service.Tier.Contract;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Workflow.Events;

namespace Lykke.Service.Tier.RabbitSubscribers
{
    public sealed class CashInSubscriber : IStartable, IDisposable
    {
        private readonly ICurrencyConverter _currencyConverter;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly ILogFactory _logFactory;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly IReadOnlyList<string> _depositCurrencies;
        private RabbitMqSubscriber<CashInEvent> _subscriber;
        private readonly ILog _log;

        public CashInSubscriber(
            ICurrencyConverter currencyConverter,
            ICqrsEngine cqrsEngine,
            ILogFactory logFactory,
            string connectionString,
            string exchangeName,
            IReadOnlyList<string> depositCurrencies
            )
        {
            _currencyConverter = currencyConverter;
            _cqrsEngine = cqrsEngine;
            _logFactory = logFactory;
            _log = _logFactory.CreateLog(this);
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _depositCurrencies = depositCurrencies;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, $"tiers-{nameof(CashInSubscriber)}")
                .MakeDurable()
                .UseRoutingKey(((int)MessageType.CashIn).ToString());

            _subscriber = new RabbitMqSubscriber<CashInEvent>(_logFactory,
                    settings,
                    new ResilientErrorHandlingStrategy(_logFactory, settings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_logFactory, settings)))
                .SetMessageDeserializer(new ProtobufMessageDeserializer<CashInEvent>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        private async Task ProcessMessageAsync(CashInEvent item)
        {
            if (!_depositCurrencies.Contains(item.CashIn.AssetId, StringComparer.InvariantCultureIgnoreCase))
                return;

            _log.Info("CashIn event", context: item.ToJson());

            double volume = Convert.ToDouble(item.CashIn.Volume);
            (double convertedVolume, string assetId) = await _currencyConverter.ConvertAsync(item.CashIn.AssetId, volume);

            if (convertedVolume == 0)
                return;

            _cqrsEngine.PublishEvent(new ClientDepositedEvent
            {
                ClientId = item.CashIn.WalletId,
                OperationId = item.Header.MessageId ?? item.Header.RequestId,
                Asset = item.CashIn.AssetId,
                Amount = volume,
                BaseAsset = assetId,
                BaseVolume = convertedVolume,
                OperationType = "CashIn",
                Timestamp = item.Header.Timestamp
            }, TierBoundedContext.Name);
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

        ~CashInSubscriber()
        {
            Dispose(false);
        }
        #endregion
    }
}
