using System;

namespace Lykke.Service.Tier.Domain.Audit
{
    public interface IAuditLogData
    {
        DateTime CreatedTime { get; }
        AuditRecordType RecordType { get; }
        string EventRecord { get; }
        string BeforeJson { get; }
        string AfterJson { get; }
        string Changer { get; }
    }
}
