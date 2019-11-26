using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Tier.Domain.Questionnaire;

namespace Lykke.Service.Tier.Domain.Repositories
{
    public interface IAnswersRepository
    {
        Task<IAnswer[]> GetAllAsync(string[] questionIds);
        Task<IAnswer[]> GetAllAsync(IEnumerable<Tuple<string, string>> ids);
        Task AddAsync(IAnswer answer);
        Task AddAnswersAsync(IEnumerable<IAnswer> answers);
        Task<IAnswer> GetAsync(string questionId, string id);
        Task DeleteAsync(string questionId, string id);
        Task DeleteAllAsync(string questionId);
    }
}
