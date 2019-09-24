using System.Threading.Tasks;

namespace Lykke.Service.Tier.Domain.Repositories
{
    public interface ILimitsRepository
    {
        Task AddAsync(string clientId, double limit, string asset);
        Task<ILimit> GetAsync(string clientId);
    }
}
