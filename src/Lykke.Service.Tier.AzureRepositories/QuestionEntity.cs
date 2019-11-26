using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Tier.Domain.Questionnaire;

namespace Lykke.Service.Tier.AzureRepositories
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class QuestionEntity : AzureTableEntity, IQuestion
    {
        public string Id => RowKey;
        public string Text { get; set; }
        public QuestionType Type { get; set; }
        public bool Required { get; set; }
        public bool HasOther { get; set; }
        public int Order { get; set; }

        public static string GeneratePk() => "Q";
        public static string GenerateRk(string id = null) => string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;

        public static QuestionEntity Create(IQuestion question)
        {
            return new QuestionEntity
            {
                PartitionKey = GeneratePk(),
                RowKey = GenerateRk(question.Id),
                Type = question.Type,
                Text = question.Text,
                Required = question.Required,
                HasOther = question.HasOther,
                Order = question.Order
            };
        }
    }
}
