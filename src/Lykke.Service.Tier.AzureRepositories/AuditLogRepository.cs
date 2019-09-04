using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.Tier.Domain.Audit;
using Lykke.Service.Tier.Domain.Repositories;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly INoSQLTableStorage<AuditLogDataEntity> _tableStorage;

        public AuditLogRepository(INoSQLTableStorage<AuditLogDataEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task InsertRecordAsync(string clientId, IAuditLogData record)
        {
            var entity = AuditLogDataEntity.Create(clientId, record);
            return _tableStorage.InsertAsync(entity);
        }
    }
}
