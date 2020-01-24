using System.Threading.Tasks;

namespace Lykke.Service.Tier.Domain
{
    public interface ICurrencyConverter
    {
        Task<(double convertedAmount, string assetId)> ConvertAsync(string assetFrom, double amount);
    }
}
