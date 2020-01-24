using System;
using Lykke.AzureStorage.Tables;
using Lykke.Service.Tier.Domain.Deposits;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class DepositOperationEntity : AzureTableEntity, IDepositOperation
    {
        public string ClientId { get; set; }
        public string FromClientId { get; set; }
        public string OperationId { get; set; }
        public string Asset { get; set; }
        public double Amount { get; set; }
        public string BaseAsset { get; set; }
        public double BaseVolume { get; set; }
        public string OperationType { get; set; }
        public DateTime Date { get; set; }

        public static string GeneratePk(string clientId) => $"{clientId}";
        public static string GenerateRk(string operationId) => operationId;

        public static DepositOperationEntity Create(IDepositOperation operation)
        {
            return new DepositOperationEntity
            {
                PartitionKey = GeneratePk(operation.ClientId),
                RowKey = GenerateRk(operation.OperationId),
                ClientId = operation.ClientId,
                FromClientId = operation.FromClientId,
                OperationId = operation.OperationId,
                Asset = operation.Asset,
                Amount = operation.Amount,
                BaseAsset = operation.BaseAsset,
                BaseVolume = operation.BaseVolume,
                OperationType = operation.OperationType,
                Date = operation.Date
            };
        }
    }
}
