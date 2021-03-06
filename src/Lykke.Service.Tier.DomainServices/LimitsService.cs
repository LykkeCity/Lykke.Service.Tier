using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Tier.Contract;
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
        private readonly ILimitsRepository _limitsRepository;
        private readonly IClientDepositsRepository _clientDepositsRepository;
        private readonly ILimitsReachedRepository _limitsReachedRepository;
        private readonly ISettingsService _settingsService;
        private readonly IReadOnlyList<string> _clientIds;
        private readonly ILog _log;

        public LimitsService(
            string instanceName,
            IDatabase database,
            ILimitsRepository limitsRepository,
            IClientDepositsRepository clientDepositsRepository,
            ILimitsReachedRepository limitsReachedRepository,
            ISettingsService settingsService,
            ILogFactory logFactory,
            IReadOnlyList<string> clientIds
            )
        {
            _instanceName = instanceName;
            _database = database;
            _limitsRepository = limitsRepository;
            _clientDepositsRepository = clientDepositsRepository;
            _limitsReachedRepository = limitsReachedRepository;
            _settingsService = settingsService;
            _clientIds = clientIds;
            _log = logFactory.CreateLog(this);
        }

        public async Task<LimitSettings> GetClientLimitSettingsAsync(string clientId, AccountTier tier, string country)
        {
            if (tier == AccountTier.Beginner)
                return null;

            var countryRisk = _settingsService.GetCountryRisk(country);

            if (countryRisk == null && !_clientIds.Contains(clientId))
            {
                _log.Warning(message: $"Can't get country risk for country {country}", context: clientId);
            }

            LimitSettings limit = null;

            if (countryRisk.HasValue)
            {
                limit = _settingsService.GetLimit(countryRisk.Value, tier);

                if (limit == null)
                {
                    _log.Warning(message: $"Can't get limit settings for tier {tier} and country risk {countryRisk}", context: clientId);
                }
            }

            var individualLimit = await _limitsRepository.GetAsync(clientId);

            var result = new LimitSettings
            {
                Tier = tier,
                MaxLimit = individualLimit?.Limit ?? limit?.MaxLimit,
                Documents = limit?.Documents ?? Array.Empty<DocumentType>()
            };

            return result;
        }

        public Task SaveDepositOperationAsync(IDepositOperation deposit)
        {
            return _clientDepositsRepository.AddAsync(deposit);
        }

        public Task DeleteDepositOperationAsync(string clientId, string operationId)
        {
            return _clientDepositsRepository.DeleteAsync(clientId, operationId);
        }

        public async Task<double> GetClientDepositAmountAsync(string clientId)
        {
            //TODO: get from redis
            var monthAgo = DateTime.UtcNow.AddDays(-30);
            var deposits = await _clientDepositsRepository.GetDepositsAsync(clientId);

            return deposits.Where(x => x.Date >= monthAgo).Sum(x =>x.BaseVolume);
        }

        public Task AddLimitAsync(string clientId, double limit, string asset)
        {
            return _limitsRepository.AddAsync(clientId, limit, asset);
        }

        public Task<ILimit> GetLimitAsync(string clientId)
        {
            return _limitsRepository.GetAsync(clientId);
        }

        public async Task<IEnumerable<IDepositOperation>> GetClientDepositsAsync(string clientId)
        {
            var monthAgo = DateTime.UtcNow.AddDays(-30);
            var depoists = await _clientDepositsRepository.GetDepositsAsync(clientId);
            return depoists.Where(x => x.Date >= monthAgo);
        }

        public Task SetLimitReachedAsync(string clientId, double amount, double maxAmount, string asset)
        {
            return _limitsReachedRepository.AddAsync(clientId, amount, maxAmount, asset);
        }

        public Task<IReadOnlyList<ILimitReached>> GetAllLimitReachedAsync()
        {
            return _limitsReachedRepository.GetAllAsync();
        }

        public Task RemoveLimitReachedAsync(string clientId)
        {
            return _limitsReachedRepository.RemoveAsync(clientId);
        }

        public async Task<bool> IsLimitReachedAsync(string clientId)
        {
            var limitReached = await _limitsReachedRepository.GetAsync(clientId);
            return limitReached != null;
        }
    }
}
