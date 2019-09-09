using System;
using Lykke.AzureStorage.Tables;
using Lykke.Service.Tier.Domain;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class LimitEntity : AzureTableEntity, ILimit
    {
        public string ClientId { get; set; }
        public double Limit { get; set; }
        public DateTime Date { get; set; }

        public static string GeneratePk(string clientId) => $"limit_{clientId}";
        public static string GenerateRk(string clientId) => clientId;

        public static LimitEntity Create(string clientId, double limit)
        {
            return new LimitEntity
            {
                PartitionKey = GeneratePk(clientId),
                RowKey = GenerateRk(clientId),
                ClientId = clientId,
                Limit = limit,
                Date = DateTime.UtcNow
            };
        }
    }
}
