using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.Tier.Domain.Repositories;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class LimitsRepository : ILimitsRepository
    {
        private readonly INoSQLTableStorage<LimitEntity> _tableStorage;

        public LimitsRepository(INoSQLTableStorage<LimitEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task AddAsync(string clientId, double limit)
        {
            return _tableStorage.InsertOrReplaceAsync(LimitEntity.Create(clientId, limit));
        }

        public async Task<double> GetAsync(string clientId)
        {
            var entity = await _tableStorage.GetDataAsync(LimitEntity.GeneratePk(clientId),
                LimitEntity.GenerateRk(clientId));

            return entity?.Limit ?? 0;
        }
    }
}
