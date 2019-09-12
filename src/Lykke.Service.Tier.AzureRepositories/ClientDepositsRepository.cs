using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.Tier.Domain.Deposits;
using Lykke.Service.Tier.Domain.Repositories;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class ClientDepositsRepository : IClientDepositsRepository
    {
        private readonly INoSQLTableStorage<DepositOperationEntity> _tableStorage;

        public ClientDepositsRepository(INoSQLTableStorage<DepositOperationEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task AddAsync(IDepositOperation operation)
        {
            return _tableStorage.InsertOrReplaceAsync(DepositOperationEntity.Create(operation));
        }

        public Task DeleteAsync(string clientId, string operationId)
        {
            return _tableStorage.DeleteIfExistAsync(DepositOperationEntity.GeneratePk(clientId),
                DepositOperationEntity.GenerateRk(operationId));
        }

        public async Task<IEnumerable<IDepositOperation>> GetDepositsAsync(string clientId)
        {
            return await _tableStorage.GetDataAsync(DepositOperationEntity.GeneratePk(clientId));
        }
    }
}
