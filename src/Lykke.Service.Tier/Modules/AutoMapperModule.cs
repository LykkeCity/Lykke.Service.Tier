using System.Collections.Generic;
using Autofac;
using AutoMapper;
using JetBrains.Annotations;
using Lykke.Service.Tier.Profiles;

namespace Lykke.Service.Tier.Modules
{
    [UsedImplicitly]
    public class AutoMapperModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ServiceProfile>().As<Profile>();

            builder.Register(c =>
            {
                var mapperConfiguration = new MapperConfiguration(cfg =>
                {
                    foreach (var profile in c.Resolve<IEnumerable<Profile>>())
                    {
                        cfg.AddProfile(profile);
                    }
                });

                mapperConfiguration.AssertConfigurationIsValid();

                return mapperConfiguration;
            }).AsSelf().SingleInstance();

            builder.Register(c => c.Resolve<MapperConfiguration>().CreateMapper(c.Resolve))
                .As<IMapper>()
                .SingleInstance();
        }
    }
}
