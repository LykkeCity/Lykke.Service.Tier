using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.Tier.Domain.Questionnaire;
using Lykke.Service.Tier.Domain.Repositories;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class UserChoicesRepository : IUserChoicesRepository
    {
        private readonly INoSQLTableStorage<UserChoiceEntity> _tableStorage;

        public UserChoicesRepository(INoSQLTableStorage<UserChoiceEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task AddAsync(string clientId, Choice[] choices)
        {
            var entities = choices.Select(x => UserChoiceEntity.Create(clientId, x));
            return _tableStorage.InsertOrMergeBatchAsync(entities);
        }

        public async Task<IChoice[]> GetClientChoicesAsync(string clientId)
        {
            return (await _tableStorage.GetDataAsync(UserChoiceEntity.GeneratePk(clientId))).ToArray();
        }
    }
}
