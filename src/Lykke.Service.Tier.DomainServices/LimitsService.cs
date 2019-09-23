using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Limitations.Client.Events;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Deposits;
using Lykke.Service.Tier.Domain.Repositories;
using Lykke.Service.Tier.Domain.Services;
using Lykke.Service.Tier.Domain.Settings;
using StackExchange.Redis;

namespace Lykke.Service.Tier.DomainServices
{
    public class LimitsService : ILimitsService
    {
        private readonly string _instanceName;
        private readonly IDatabase _database;
        private readonly Dictionary<CountryRisk, LimitSettings[]> _limitSettings;
        private readonly int[] _pushLimitsSettings;
        private readonly ILimitsRepository _limitsRepository;
        private readonly IClientDepositsRepository _clientDepositsRepository;
        private readonly IMapper _mapper;
        private readonly ICountriesService _countriesService;
        private readonly ILog _log;

        public LimitsService(
            string instanceName,
            IDatabase database,
            Dictionary<CountryRisk, LimitSettings[]> limitSettings,
            int[] pushLimitsSettings,
            ILimitsRepository limitsRepository,
            IClientDepositsRepository clientDepositsRepository,
            IMapper mapper,
            ICountriesService countriesService,
            ILogFactory logFactory
            )
        {
            _instanceName = instanceName;
            _database = database;
            _limitSettings = limitSettings;
            _pushLimitsSettings = pushLimitsSettings;
            _limitsRepository = limitsRepository;
            _clientDepositsRepository = clientDepositsRepository;
            _mapper = mapper;
            _countriesService = countriesService;
            _log = logFactory.CreateLog(this);
        }

        public async Task<LimitSettings> GetClientLimitSettingsAsync(string clientId, AccountTier tier, string country)
        {
            if (tier == AccountTier.Beginner)
                return null;

            var countryRisk = _countriesService.GetCountryRisk(country);

            if (countryRisk == null)
            {
                _log.Error(message: $"Can't get country risk for country {country}");
                return null;
            }

            LimitSettings limit = _limitSettings.ContainsKey(countryRisk.Value)
                ? _limitSettings[countryRisk.Value].FirstOrDefault(x => x.Tier == tier)
                : null;

            if (limit == null)
            {
                _log.Error(message: $"Can't get limit settings for tier {tier} and country risk {countryRisk}");
                return null;
            }

            if (limit.MaxLimit != null)
                return limit;

            var individualLimit = await _limitsRepository.GetAsync(clientId);

            if (individualLimit == 0)
            {
                _log.Error(message: "No individual limit", context: new {clientId});
            }

            limit.MaxLimit = individualLimit;

            return limit;
        }

        public bool IsLimitReachedForNotification(double current, double max)
        {
            var currentPercent = current / max * 100;

            bool result = false;

            foreach (var percent in _pushLimitsSettings.OrderBy(x => x))
            {
                if (currentPercent >= percent)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        public async Task SaveDepositOperationAsync(ClientDepositEvent evt)
        {
            var operation = _mapper.Map<DepositOperation>(evt);

            await _clientDepositsRepository.AddAsync(operation);

            //TODO: add to redis
        }

        public Task DeleteDepositOperationAsync(string clientId, string operationId)
        {
            return _clientDepositsRepository.DeleteAsync(clientId, operationId);
        }

        public async Task<double> GetClientDepositAmountAsync(string clientId, AccountTier tier)
        {
            //TODO: get from redis
            var monthAgo = DateTime.UtcNow.AddMonths(-1);
            var deposits = await _clientDepositsRepository.GetDepositsAsync(clientId);

            return tier == AccountTier.Apprentice
                ? deposits.Sum(x => x.BaseVolume)
                : deposits.Where(x => x.Date >= monthAgo).Sum(x =>x.BaseVolume);
        }
    }
}
