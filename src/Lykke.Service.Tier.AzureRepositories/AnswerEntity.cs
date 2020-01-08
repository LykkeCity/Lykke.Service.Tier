using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Tier.Domain.Questionnaire;

namespace Lykke.Service.Tier.AzureRepositories
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class AnswerEntity : AzureTableEntity, IAnswer
    {
        public string Id => RowKey;
        public string QuestionId => PartitionKey;
        public string Text { get; set; }
        public int Order { get; set; }
        public double Weight { get; set; }

        public static string GeneratePk(string questionId) => questionId;
        public static string GenerateRk(string id = null) => string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;

        public static AnswerEntity Create(IAnswer answer)
        {
            return new AnswerEntity
            {
                PartitionKey = GeneratePk(answer.QuestionId),
                RowKey = GenerateRk(answer.Id),
                Text = answer.Text,
                Order = answer.Order,
                Weight = answer.Weight
            };
        }
    }
}
