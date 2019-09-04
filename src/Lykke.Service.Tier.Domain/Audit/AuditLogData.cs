using System;

namespace Lykke.Service.Tier.Domain.Audit
{
    public class AuditLogData : IAuditLogData
    {
        public DateTime CreatedTime { get; set; }
        public AuditRecordType RecordType { get; set; }
        public string EventRecord { get; set; }
        public string BeforeJson { get; set; }
        public string AfterJson { get; set; }
        public string Changer { get; set; }
    }
}
