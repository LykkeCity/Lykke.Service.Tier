using AutoMapper;
using JetBrains.Annotations;
using Lykke.Service.Limitations.Client.Events;
using Lykke.Service.Tier.AzureRepositories;
using Lykke.Service.Tier.Client.Models.Responses;
using Lykke.Service.Tier.Contract;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Deposits;

namespace Lykke.Service.Tier.Profiles
{
    [UsedImplicitly]
    public class ServiceProfile : Profile
    {
        public ServiceProfile()
        {
            CreateMap<ITierUpgradeRequest, TierUpgradeRequestResponse>(MemberList.Destination);
            CreateMap<TierUpgradeRequestEntity, TierUpgradeRequestResponse>(MemberList.Destination);
            CreateMap<ClientDepositEvent, DepositOperation>(MemberList.Destination)
                .ForMember(x => x.Date, o => o.MapFrom(x=>x.Timestamp));
            CreateMap<ILimit, LimitResponse>(MemberList.Destination);
            CreateMap<AccountTier, TierModel>(MemberList.Destination);
        }
    }
}
