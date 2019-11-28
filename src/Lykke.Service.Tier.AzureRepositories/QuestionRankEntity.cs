using System;
using Common;
using Lykke.AzureStorage.Tables;
using Lykke.Service.Tier.Domain.Questionnaire;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class QuestionRankEntity : AzureTableEntity, IQuestionRank
    {
        public string ClientId { get; set; }
        public double Rank { get; set; }
        public string Changer { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        public static string GeneratePk(string clientId) => clientId;
        public static string GenerateRk() => IdGenerator.GenerateDateTimeIdNewFirst(DateTime.UtcNow);

        public static QuestionRankEntity Create(string clientId, double rank, string changer, string comment)
        {
            return new QuestionRankEntity
            {
                PartitionKey = GeneratePk(clientId),
                RowKey = GenerateRk(),
                ClientId = clientId,
                Rank = rank,
                Changer = changer,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
