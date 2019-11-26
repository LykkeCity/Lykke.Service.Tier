using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.Tier.Domain.Questionnaire;
using Lykke.Service.Tier.Domain.Repositories;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class QuestionsRepository : IQuestionsRepository
    {
        private readonly INoSQLTableStorage<QuestionEntity> _tableStorage;

        public QuestionsRepository(INoSQLTableStorage<QuestionEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IQuestion[]> GetAllAsync()
        {
            return (await _tableStorage.GetDataAsync(QuestionEntity.GeneratePk())).ToArray();
        }

        public async Task<IQuestion[]> GetAllAsync(string[] ids)
        {
            return (await _tableStorage.GetDataAsync(QuestionEntity.GeneratePk(), ids)).ToArray();
        }

        public async Task<IQuestion> GetAsync(string id)
        {
            return await _tableStorage.GetDataAsync(QuestionEntity.GeneratePk(), QuestionEntity.GenerateRk(id));
        }

        public async Task<string> AddAsync(IQuestion question)
        {
            var entity = QuestionEntity.Create(question);
            await _tableStorage.InsertOrMergeAsync(entity);
            return entity.Id;
        }

        public Task DeleteAsync(string id)
        {
            return _tableStorage.DeleteIfExistAsync(QuestionEntity.GeneratePk(), QuestionEntity.GenerateRk(id));
        }
    }
}
