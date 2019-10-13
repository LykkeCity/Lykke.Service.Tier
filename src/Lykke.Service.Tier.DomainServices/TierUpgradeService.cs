using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Cqrs;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.ClientAccount.Client.Models.Request.ClientAccount;
using Lykke.Service.ClientAccount.Client.Models.Request.Settings;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Tier.Contract;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Audit;
using Lykke.Service.Tier.Domain.Events;
using Lykke.Service.Tier.Domain.Repositories;
using Lykke.Service.Tier.Domain.Services;
using StackExchange.Redis;

namespace Lykke.Service.Tier.DomainServices
{
    public class TierUpgradeService : ITierUpgradeService
    {
        public ICqrsEngine CqrsEngine { get; set; }

        private readonly string _instanceName;
        private readonly IDatabase _cache;
        private readonly ITierUpgradeRequestsRepository _repository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IClientAccountClient _clientAccountClient;

        public TierUpgradeService(
            string instanceName,
            IDatabase database,
            ITierUpgradeRequestsRepository repository,
            IAuditLogRepository auditLogRepository,
            IClientAccountClient clientAccountClient
        )
        {
            _instanceName = instanceName;
            _cache = database;
            _repository = repository;
            _auditLogRepository = auditLogRepository;
            _clientAccountClient = clientAccountClient;
        }

        public async Task AddAsync(string clientId, AccountTier tier, KycStatus status, string changer, string comment = null)
        {
            ITierUpgradeRequest currentTierRequest = await GetAsync(clientId, tier);

            await _repository.AddAsync(clientId, tier, status);

            await _auditLogRepository.InsertRecordAsync(clientId, new AuditLogData
            {
                BeforeJson = GetStatus(tier, currentTierRequest).ToJson(),
                AfterJson = (currentTierRequest?.KycStatus == status ? $"{tier}:{status} (updated)" : $"{tier}:{status}").ToJson(),
                CreatedTime = DateTime.UtcNow,
                RecordType = AuditRecordType.TierUpgradeRequest,
                Changer = changer
            });

            if (status == KycStatus.Ok)
            {
                await _clientAccountClient.ClientAccount.ChangeAccountTierAsync(clientId, new AccountTierRequest{ Tier = tier});
                await _clientAccountClient.ClientSettings.SetCashOutBlockAsync(new CashOutBlockRequest
                {
                    ClientId = clientId,
                    CashOutBlocked = false,
                    TradesBlocked = false
                });
            }

            if (currentTierRequest?.KycStatus != status)
            {
                CqrsEngine.PublishEvent(new TierUpgradeRequestChangedEvent
                {
                    ClientId = clientId,
                    Tier = tier,
                    OldStatus = currentTierRequest?.KycStatus,
                    NewStatus = status
                }, TierBoundedContext.Name);
            }
        }

        public Task UpdateCountsAsync(string clientId, AccountTier tier, KycStatus? oldStatus, KycStatus newStatus)
        {
            if (newStatus == KycStatus.Ok && oldStatus.HasValue)
                return DecrementCountAsync(tier);

            if (!oldStatus.HasValue)
                return IncrementCountAsync(tier);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ITierUpgradeRequest>> GetByTierAsync(AccountTier tier)
        {
            return _repository.GetByTierAsync(tier);
        }

        public Task<ITierUpgradeRequest> GetAsync(string clientId, AccountTier tier)
        {
            return _repository.GetAsync(clientId, tier);
        }

        public Task<IReadOnlyList<ITierUpgradeRequest>> GetAsync(string clientId)
        {
            return _repository.GetAsync(clientId);
        }

        public async Task<Dictionary<string, int>> GetCountsAsync()
        {
            var result = new Dictionary<string, int>
            {
                { AccountTier.Advanced.ToString(), 0 },
                { AccountTier.ProIndividual.ToString(), 0 }
            };

            HashEntry[] redisValues = await _cache.HashGetAllAsync(GetCountsCacheKey());

            if (redisValues.Any())
            {
                foreach (var redisValue in redisValues)
                {
                    result[redisValue.Name] = Convert.ToInt32(redisValue.Value);
                }
            }
            else
            {
                result = await InitCache();
            }

            return result;
        }

        public async Task<Dictionary<string, int>> InitCache()
        {
            var result = await _repository.GetCountsAsync();
            await SetCountsInCacheAsync(result);
            return result;
        }

        private async Task IncrementCountAsync(AccountTier tier)
        {
            int count = await _repository.GetCountAsync(tier);
            count += 1;

            await Task.WhenAll(
                _repository.AddCountAsync(tier, count),
                SetCountInCacheAsync(tier, count)
            );
        }

        private async Task DecrementCountAsync(AccountTier tier)
        {
            int count = await _repository.GetCountAsync(tier);

            int value = Math.Max(0, count - 1);

            await Task.WhenAll(
                _repository.AddCountAsync(tier, value),
                SetCountInCacheAsync(tier, value)
            );
        }

        private Task SetCountInCacheAsync(AccountTier tier, int count)
        {
            return _cache.HashSetAsync(GetCountsCacheKey(), new List<HashEntry>
            {
                new HashEntry(tier.ToString(), count.ToString())
            }.ToArray());
        }

        private Task SetCountsInCacheAsync(Dictionary<string, int> counts)
        {
            HashEntry[] hashEntities = counts.Select(x => new HashEntry(x.Key, x.Value.ToString())).ToArray();

            return _cache.HashSetAsync(GetCountsCacheKey(), hashEntities);
        }

        private static string GetStatus(AccountTier tier, ITierUpgradeRequest request)
        {
            return request == null ? string.Empty : $"{tier}:{request.KycStatus.ToString()}";
        }

        private string GetCountsCacheKey() => $"{_instanceName}:upgraderequest:counts";
    }
}
