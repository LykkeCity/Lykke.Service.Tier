using System.Threading.Tasks;
using Lykke.Service.Tier.Domain.Audit;

namespace Lykke.Service.Tier.Domain.Repositories
{
    public interface IAuditLogRepository
    {
        Task InsertRecordAsync(string clientId, IAuditLogData record);
    }
}
