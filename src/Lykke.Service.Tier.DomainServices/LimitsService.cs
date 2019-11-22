using System;
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
        private readonly ILimitsRepository _limitsRepository;
        private readonly IClientDepositsRepository _clientDepositsRepository;
        private readonly IMapper _mapper;
        private readonly ISettingsService _settingsService;
        private readonly ILog _log;

        public LimitsService(
            string instanceName,
            IDatabase database,
            ILimitsRepository limitsRepository,
            IClientDepositsRepository clientDepositsRepository,
            IMapper mapper,
            ISettingsService settingsService,
            ILogFactory logFactory
            )
        {
            _instanceName = instanceName;
            _database = database;
            _limitsRepository = limitsRepository;
            _clientDepositsRepository = clientDepositsRepository;
            _mapper = mapper;
            _settingsService = settingsService;
            _log = logFactory.CreateLog(this);
        }

        public async Task<LimitSettings> GetClientLimitSettingsAsync(string clientId, AccountTier tier, string country)
        {
            if (tier == AccountTier.Beginner)
                return null;

            var countryRisk = _settingsService.GetCountryRisk(country);

            if (countryRisk == null)
            {
                _log.Error(message: $"Can't get country risk for country {country}");
                return null;
            }

            LimitSettings limit = _settingsService.GetLimit(countryRisk.Value, tier);

            if (limit == null)
            {
                _log.Error(message: $"Can't get limit settings for tier {tier} and country risk {countryRisk}");
                return null;
            }

            var individualLimit = await _limitsRepository.GetAsync(clientId);

            var result = new LimitSettings
            {
                Tier = limit.Tier,
                MaxLimit = individualLimit?.Limit ?? limit.MaxLimit,
                Documents = limit.Documents
            };

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
    }
}
