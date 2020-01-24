using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Services;

namespace Lykke.Service.Tier.DomainServices
{
    public class CurrencyConverter : ICurrencyConverter
    {
        private readonly ISettingsService _settingsService;
        private readonly IRateCalculatorClient _rateCalculatorClient;
        private readonly ILog _log;

        public CurrencyConverter(
            ISettingsService settingsService,
            IRateCalculatorClient rateCalculatorClient,
            ILogFactory logFactory
            )
        {
            _settingsService = settingsService;
            _rateCalculatorClient = rateCalculatorClient;
            _log = logFactory.CreateLog(this);
        }
        public async Task<(double convertedAmount, string assetId)> ConvertAsync(string assetFrom, double amount)
        {
            var defaultAsset = _settingsService.GetDefaultAsset();

            if(assetFrom == defaultAsset)
                return (amount, defaultAsset);

            var convertedAmount = await _rateCalculatorClient.GetAmountInBaseAsync(assetFrom, amount, defaultAsset);

            if (amount != 0 && convertedAmount == 0)
            {
                _log.Warning($"Conversion from {amount} {assetFrom} to {defaultAsset} resulted in 0.");
            }

            return (convertedAmount, defaultAsset);
        }
    }
}
