using System.Threading.Tasks;
using Lykke.Service.Tier.Domain.Questionnaire;

namespace Lykke.Service.Tier.Domain.Repositories
{
    public interface IQuestionsRepository
    {
        Task<IQuestion[]> GetAllAsync();
        Task<IQuestion[]> GetAllAsync(string[] ids);
        Task<IQuestion> GetAsync(string id);
        Task<string> AddAsync(IQuestion question);
        Task DeleteAsync(string id);
    }
}
