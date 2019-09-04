using System;
using Common;
using Lykke.Service.Tier.Domain.Audit;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class AuditLogDataEntity : TableEntity, IAuditLogData
    {
        public DateTime CreatedTime { get; set; }
        public AuditRecordType RecordType { get; set; }
        public string EventRecord { get; set; }
        public string BeforeJson { get; set; }
        public string AfterJson { get; set; }
        public string Changer { get; set; }

        public static string GeneratePk(string clientId, AuditRecordType recordType) => $"AUD_{clientId}_{(int)recordType}";
        public static string GenerateRk(DateTime creationDt) => IdGenerator.GenerateDateTimeIdNewFirst(creationDt);

        public static AuditLogDataEntity Create(string clientId, IAuditLogData data)
        {
            return new AuditLogDataEntity
            {
                PartitionKey = GeneratePk(clientId, data.RecordType),
                RowKey = GenerateRk(data.CreatedTime),
                CreatedTime = data.CreatedTime,
                BeforeJson = data.BeforeJson,
                AfterJson = data.AfterJson,
                EventRecord = data.EventRecord,
                Changer = data.Changer
            };
        }

    }
}
