using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Repositories;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class LimitsReachedRepository : ILimitsReachedRepository
    {
        private readonly INoSQLTableStorage<LimitReachedEntity> _tableStorage;

        public LimitsReachedRepository(INoSQLTableStorage<LimitReachedEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task AddAsync(string clientId, double amount, double maxAmount, string asset)
        {
            return _tableStorage.InsertOrReplaceAsync(LimitReachedEntity.Create(clientId, amount, maxAmount, asset));
        }

        public Task RemoveAsync(string clientId)
        {
            return _tableStorage.DeleteIfExistAsync(LimitReachedEntity.GeneratePk(),
                LimitReachedEntity.GenerateRk(clientId));
        }

        public async Task<ILimitReached> GetAsync(string clientId)
        {
            var entity = await _tableStorage.GetDataAsync(LimitReachedEntity.GeneratePk(),
                LimitReachedEntity.GenerateRk(clientId));

            return entity;
        }

        public async Task<IReadOnlyList<ILimitReached>> GetAllAsync()
        {
            var entities = await _tableStorage.GetDataAsync(LimitReachedEntity.GeneratePk());
            return entities.ToList();
        }
    }
}
