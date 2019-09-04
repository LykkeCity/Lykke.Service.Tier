using AutoMapper;
using JetBrains.Annotations;
using Lykke.Service.Tier.AzureRepositories;
using Lykke.Service.Tier.Client.Models.Responses;
using Lykke.Service.Tier.Domain;

namespace Lykke.Service.Tier.Profiles
{
    [UsedImplicitly]
    public class ServiceProfile : Profile
    {
        public ServiceProfile()
        {
            CreateMap<ITierUpgradeRequest, TierUpgradeRequestResponse>(MemberList.Destination);
            CreateMap<TierUpgradeRequestEntity, TierUpgradeRequestResponse>(MemberList.Destination);
        }
    }
}
