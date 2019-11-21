using System.Threading.Tasks;
using Lykke.Service.Tier.Contract;

namespace Lykke.Service.Tier.Domain.Services
{
    public interface ITiersService
    {
        Task<ClientTierInfo> GetClientTierInfoAsync(string clientId, AccountTier clientTier, string country);
    }
}
