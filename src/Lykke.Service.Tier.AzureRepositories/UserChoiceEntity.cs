using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Tier.Domain.Questionnaire;

namespace Lykke.Service.Tier.AzureRepositories
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class UserChoiceEntity : AzureTableEntity, IChoice
    {
        public string QuestionId { get; set; }

        [ValueSerializer(typeof(JsonStorageValueSerializer))]
        public string[] AnswerIds { get; set; }
        public string Other { get; set; }

        public static string GeneratePk(string clientId) => clientId;
        public static string GenerateRk(string questionId) => questionId;

        public static UserChoiceEntity Create(string clientId, IChoice choice)
        {
            return new UserChoiceEntity
            {
                PartitionKey = GeneratePk(clientId),
                RowKey = GenerateRk(choice.QuestionId),
                QuestionId = choice.QuestionId,
                AnswerIds = choice.AnswerIds,
                Other = choice.Other
            };
        }
    }
}
