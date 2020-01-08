using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.Tier.Domain.Questionnaire;
using Lykke.Service.Tier.Domain.Repositories;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class AnswersRepository : IAnswersRepository
    {
        private readonly INoSQLTableStorage<AnswerEntity> _tableStorage;

        public AnswersRepository(INoSQLTableStorage<AnswerEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IAnswer[]> GetAllAsync(string[] questionIds)
        {
            var partitionKeys = questionIds.Select(AnswerEntity.GeneratePk).ToList();
            return (await _tableStorage.GetDataAsync(partitionKeys)).ToArray();
        }

        public async Task<IAnswer[]> GetAllAsync(IEnumerable<Tuple<string, string>> ids)
        {
            return (await _tableStorage.GetDataAsync(ids)).ToArray();
        }

        public Task AddAsync(IAnswer answer)
        {
            return _tableStorage.InsertOrMergeAsync(AnswerEntity.Create(answer));
        }

        public Task AddAnswersAsync(IEnumerable<IAnswer> answers)
        {
            return _tableStorage.InsertOrMergeBatchAsync(answers.Select(AnswerEntity.Create));
        }

        public async Task<IAnswer> GetAsync(string questionId, string id)
        {
            return await _tableStorage.GetDataAsync(AnswerEntity.GeneratePk(questionId), AnswerEntity.GenerateRk(id));
        }

        public Task DeleteAsync(string questionId, string id)
        {
            return _tableStorage.DeleteIfExistAsync(AnswerEntity.GeneratePk(questionId), AnswerEntity.GenerateRk(id));
        }

        public async Task DeleteAllAsync(string questionId)
        {
            var answers = await _tableStorage.GetDataAsync(AnswerEntity.GeneratePk(questionId));
            await _tableStorage.DeleteAsync(answers);
        }
    }
}
