using System;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.ClientAccount.Client.Models.Request.Settings;
using Lykke.Service.ClientAccount.Client.Models.Response.ClientAccountInformation;
using Lykke.Service.Limitations.Client.Events;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Contract.Models;
using Lykke.Service.Tier.Settings;

namespace Lykke.Service.Tier.Workflow.Projections
{
    public class ClientDepositsProjection
    {
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly CountriesSettings _countriesSettings;

        public ClientDepositsProjection(
            IClientAccountClient clientAccountClient,
            IPersonalDataService personalDataService,
            CountriesSettings countriesSettings
            )
        {
            _clientAccountClient = clientAccountClient;
            _personalDataService = personalDataService;
            _countriesSettings = countriesSettings;
        }

        public async Task Handle(ClientDepositEvent evt)
        {
            var clientAccountTask = _clientAccountClient.ClientAccountInformation.GetByIdAsync(evt.ClientId);
            var pdTask = _personalDataService.GetAsync(evt.ClientId);

            await Task.WhenAll(clientAccountTask, pdTask);

            ClientInfo clientAccount = clientAccountTask.Result;
            IPersonalData pd = pdTask.Result;

            double currentMaxLimit = await GetClientLimitSettingsAsync(evt.ClientId, clientAccount.Tier, pd.CountryFromPOA);
            var checkAmount = clientAccount.Tier == AccountTier.Apprentice ? evt.Total : evt.TotalMonth;


            if (Math.Abs(checkAmount - currentMaxLimit) < 0.01)
            {
                await _clientAccountClient.ClientSettings.SetCashOutBlockAsync(new CashOutBlockRequest
                {
                    ClientId = evt.ClientId, CashOutBlocked = false, TradesBlocked = false
                });
            }

            if (checkAmount > currentMaxLimit)
            {
                await _clientAccountClient.ClientSettings.SetCashOutBlockAsync(new CashOutBlockRequest
                {
                    ClientId = evt.ClientId, CashOutBlocked = false, TradesBlocked = true
                });
            }
        }

        private async Task<double> GetClientLimitSettingsAsync(string clientId, AccountTier tier, string country)
        {
            throw new System.NotImplementedException();
        }
    }
}
