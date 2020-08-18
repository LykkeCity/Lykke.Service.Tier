using System;
using Lykke.AzureStorage.Tables;
using Lykke.Service.Tier.Domain;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class LimitReachedEntity : AzureTableEntity, ILimitReached
    {
        public string ClientId { get; set; }
        public double Amount { get; set; }
        public double MaxAmount { get; set; }
        public string Asset { get; set; }
        public DateTime Date { get; set; }

        public static string GeneratePk() => "LimitReached";
        public static string GenerateRk(string clientId) => clientId;

        public static LimitReachedEntity Create(string clientId, double amount, double maxAmount, string asset)
        {
            return new LimitReachedEntity
            {
                PartitionKey = GeneratePk(),
                RowKey = GenerateRk(clientId),
                ClientId = clientId,
                Amount= amount,
                MaxAmount = maxAmount,
                Asset = asset,
                Date = DateTime.UtcNow
            };
        }
    }
}
