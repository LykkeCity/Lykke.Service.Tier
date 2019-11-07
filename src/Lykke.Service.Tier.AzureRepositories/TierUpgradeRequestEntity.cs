using System;
using Lykke.AzureStorage.Tables;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Tier.Domain;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class TierUpgradeRequestEntity : AzureTableEntity, ITierUpgradeRequest
    {
        public string ClientId { get; set; }
        public AccountTier Tier { get; set; }
        public KycStatus KycStatus { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
        public int Count { get; set; }

        public static string GeneratePk(AccountTier tier) => $"{tier}";
        public static string GenerateRk(string clientId) => clientId;
        public static string GenerateCountPk(AccountTier tier) => $"{tier}Count";
        public static string GenerateCountRk() => "Count";

        public static TierUpgradeRequestEntity Create(string clientId, AccountTier tier, KycStatus status, string comment, DateTime? date = null)
        {
            return new TierUpgradeRequestEntity
            {
                PartitionKey = GeneratePk(tier),
                RowKey = GenerateRk(clientId),
                ClientId = clientId,
                Tier = tier,
                KycStatus = status,
                Date = date ?? DateTime.UtcNow,
                Comment = comment
            };
        }

        public static TierUpgradeRequestEntity CreateCount(AccountTier tier, int count)
        {
            return new TierUpgradeRequestEntity
            {
                PartitionKey = GenerateCountPk(tier),
                RowKey = GenerateCountRk(),
                Tier = tier,
                Count = count,
                Date = DateTime.UtcNow
            };
        }
    }
}
