using System.Threading.Tasks;

namespace Lykke.Service.Tier.Domain.Repositories
{
    public interface ILimitsRepository
    {
        Task AddAsync(string clientId, double limit);
        Task<double> GetAsync(string clientId);
    }
}
