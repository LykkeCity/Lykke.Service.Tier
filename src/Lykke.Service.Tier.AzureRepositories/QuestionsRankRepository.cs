using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.Tier.Domain.Questionnaire;
using Lykke.Service.Tier.Domain.Repositories;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class QuestionsRankRepository : IQuestionsRankRepository
    {
        private readonly INoSQLTableStorage<QuestionRankEntity> _tableStorage;

        public QuestionsRankRepository(INoSQLTableStorage<QuestionRankEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task AddAsync(string clientId, double rank, string changer, string comment)
        {
            return _tableStorage.TryInsertAsync(QuestionRankEntity.Create(clientId, rank, changer, comment));
        }

        public async Task<IQuestionRank> GetAsync(string clientId)
        {
            return await _tableStorage.GetTopRecordAsync(QuestionRankEntity.GeneratePk(clientId));
        }

        public async Task<IReadOnlyList<IQuestionRank>> GetAllAsync(string clientId)
        {
            return (await _tableStorage.GetDataAsync(QuestionRankEntity.GeneratePk(clientId))).ToList();
        }
    }
}
