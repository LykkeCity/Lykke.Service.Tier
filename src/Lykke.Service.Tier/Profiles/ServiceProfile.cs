using AutoMapper;
using JetBrains.Annotations;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Limitations.Client.Events;
using Lykke.Service.Tier.AzureRepositories;
using Lykke.Service.Tier.Client.Models;
using Lykke.Service.Tier.Client.Models.Requests;
using Lykke.Service.Tier.Client.Models.Responses;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Deposits;
using Lykke.Service.Tier.Domain.Questionnaire;

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
            CreateMap<QuestionEntity, Question>(MemberList.Destination)
                .ForMember(d => d.Answers, o => o.Ignore());
            CreateMap<Question, QuestionModel>(MemberList.Destination);
            CreateMap<QuestionType, QuestionTypeModel>(MemberList.Destination);
            CreateMap<QuestionRequest, Question>(MemberList.Source)
                .ForMember(d => d.Answers, o => o.Ignore());
            CreateMap<QuestionUpdateRequest, Question>(MemberList.Destination)
                .ForMember(d => d.Answers, o => o.Ignore());
            CreateMap<AnswerUpdateRequest, Answer>(MemberList.Destination)
                .ForMember(d => d.Weight, o => o.Ignore());
            CreateMap<ChoiceModel, Choice>(MemberList.Destination);
            CreateMap<IQuestionRank, QuestionnaireRankResponse>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.CreatedAt));
        }
    }
}
