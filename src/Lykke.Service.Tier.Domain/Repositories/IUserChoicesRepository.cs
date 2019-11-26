using System.Threading.Tasks;
using Lykke.Service.Tier.Domain.Questionnaire;

namespace Lykke.Service.Tier.Domain.Repositories
{
    public interface IUserChoicesRepository
    {
        Task AddAsync(string clientId, Choice[] choices);
        Task<IChoice[]> GetClientChoicesAsync(string clientId);
    }
}
