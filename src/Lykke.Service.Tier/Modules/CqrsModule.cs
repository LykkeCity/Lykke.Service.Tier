using System.Collections.Generic;
using Autofac;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Cqrs.Middleware.Logging;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using Lykke.Service.Limitations.Client;
using Lykke.Service.Limitations.Client.Events;
using Lykke.Service.PushNotifications.Contract;
using Lykke.Service.PushNotifications.Contract.Commands;
using Lykke.Service.Tier.Contract;
using Lykke.Service.Tier.Domain.Events;
using Lykke.Service.Tier.Settings;
using Lykke.Service.Tier.Workflow.Projections;
using Lykke.SettingsReader;

namespace Lykke.Service.Tier.Modules
{
    [UsedImplicitly]
    public class CqrsModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public CqrsModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            MessagePackSerializerFactory.Defaults.FormatterResolver = MessagePack.Resolvers.ContractlessStandardResolver.Instance;
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory { Uri = _settings.CurrentValue.TierService.Cqrs.RabbitConnectionString };

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>();

            builder.Register(ctx => new MessagingEngine(ctx.Resolve<ILogFactory>(),
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {
                        "Transports",
                        new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName,
                            rabbitMqSettings.Password, "None", "RabbitMq")
                    }
                }),
                new RabbitMqTransportFactory(ctx.Resolve<ILogFactory>()))).As<IMessagingEngine>().SingleInstance();

            builder.RegisterType<ClientDepositsProjection>();
            builder.RegisterType<TierUpgradeRequestProjection>().PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            const string environment = "lykke";
            const string queuePostfix = "k8s";

            builder.Register(ctx =>
            {
                var engine = new CqrsEngine(ctx.Resolve<ILogFactory>(),
                    ctx.Resolve<IDependencyResolver>(),
                    ctx.Resolve<IMessagingEngine>(),
                    new DefaultEndpointProvider(),
                    true,
                    Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                        "Transports",
                        SerializationFormat.MessagePack,
                        environment: environment,
                        exclusiveQueuePostfix: queuePostfix)),

                    Register.EventInterceptors(new DefaultEventLoggingInterceptor(ctx.Resolve<ILogFactory>())),

                    Register.BoundedContext(TierBoundedContext.Name)
                        .ListeningEvents(typeof(ClientDepositEvent))
                            .From(LimitationsBoundedContext.Name).On("events")
                            .WithProjection(typeof(ClientDepositsProjection), LimitationsBoundedContext.Name)

                        .PublishingEvents(typeof (TierUpgradeRequestChangedEvent))
                            .With("self")
                            .WithLoopback()

                        .ListeningEvents(typeof(TierUpgradeRequestChangedEvent))
                            .From(TierBoundedContext.Name).On("self")
                            .WithProjection(typeof(TierUpgradeRequestProjection), TierBoundedContext.Name)

                        .PublishingCommands(typeof(TextNotificationCommand))
                            .To(PushNotificationsBoundedContext.Name)
                            .With("commands")
                );

                engine.StartPublishers();

                return engine;
            })
            .As<ICqrsEngine>()
            .SingleInstance()
            .AutoActivate();
        }
    }
}
