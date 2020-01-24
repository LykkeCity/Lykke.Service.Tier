using System;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.MatchingEngine.Connector.Models.Events.Common;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Service.Tier.RabbitSubscribers
{
    public sealed class CashInSubscriber : IStartable, IDisposable
    {
        private readonly ILogFactory _logFactory;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private RabbitMqSubscriber<CashInEvent> _subscriber;
        private readonly ILog _log;

        public CashInSubscriber(
            ILogFactory logFactory,
            string connectionString,
            string exchangeName
            )
        {
            _logFactory = logFactory;
            _log = _logFactory.CreateLog(this);
            _connectionString = connectionString;
            _exchangeName = exchangeName;
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

        private Task ProcessMessageAsync(CashInEvent item)
        {
            _log.Info("CashIn event", context: item.ToJson());
            return Task.CompletedTask;
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
