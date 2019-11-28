using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Tier.Domain.Questionnaire;

namespace Lykke.Service.Tier.Domain.Repositories
{
    public interface IQuestionsRankRepository
    {
        Task AddAsync(string clientId, double rank, string changer, string comment);
        Task<IQuestionRank> GetAsync(string clientId);
        Task<IReadOnlyList<IQuestionRank>> GetAllAsync(string clientId);
    }
}
