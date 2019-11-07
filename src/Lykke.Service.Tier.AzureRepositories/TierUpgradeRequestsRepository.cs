using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Repositories;

namespace Lykke.Service.Tier.AzureRepositories
{
    public class TierUpgradeRequestsRepository : ITierUpgradeRequestsRepository
    {
        private readonly INoSQLTableStorage<TierUpgradeRequestEntity> _tableStorage;
        private readonly INoSQLTableStorage<AzureIndex> _index;
        private const string PendingRequestsIndex = "PendingRequests";

        public TierUpgradeRequestsRepository(
            INoSQLTableStorage<TierUpgradeRequestEntity> tableStorage,
            INoSQLTableStorage<AzureIndex> index)
        {
            _tableStorage = tableStorage;
            _index = index;
        }

        public async Task AddAsync(string clientId, AccountTier tier, KycStatus status, string comment = null, DateTime? date = null)
        {
            var item = TierUpgradeRequestEntity.Create(clientId, tier, status, comment, date);
            await _tableStorage.InsertOrReplaceAsync(item);

            if (status == KycStatus.Pending)
            {
                var indexEntity = AzureIndex.Create(PendingRequestsIndex, GetPendingRequestIndexRk(clientId, tier), item);
                await _index.InsertOrMergeAsync(indexEntity);
            }
        }

        public async Task<ITierUpgradeRequest> GetAsync(string clientId, AccountTier tier)
        {
            return await _tableStorage.GetDataAsync(TierUpgradeRequestEntity.GeneratePk(tier),
                TierUpgradeRequestEntity.GenerateRk(clientId));
        }

        public async Task<IReadOnlyList<ITierUpgradeRequest>> GetByClientAsync(string clientId)
        {
            return (await _tableStorage.GetDataAsync(new[]
            {
                new Tuple<string, string>(TierUpgradeRequestEntity.GeneratePk(AccountTier.Apprentice), clientId),
                new Tuple<string, string>(TierUpgradeRequestEntity.GeneratePk(AccountTier.Advanced), clientId),
                new Tuple<string, string>(TierUpgradeRequestEntity.GeneratePk(AccountTier.ProIndividual), clientId)
            })).ToList();
        }

        public async Task<IReadOnlyList<ITierUpgradeRequest>> GetByTierAsync(AccountTier tier)
        {
            return (await _tableStorage.GetDataAsync(TierUpgradeRequestEntity.GeneratePk(tier))).ToList();
        }

        public async Task<IReadOnlyList<ITierUpgradeRequest>> GetPendingRequestsAsync()
        {
            IEnumerable<AzureIndex> indexes = await _index.GetDataAsync(PendingRequestsIndex);
            return (await _tableStorage.GetDataAsync(indexes)).ToList();
        }

        public Task DeletePendingRequestIndexAsync(string clientId, AccountTier tier)
        {
            return _index.DeleteIfExistAsync(PendingRequestsIndex, GetPendingRequestIndexRk(clientId, tier));
        }

        public Task AddCountAsync(AccountTier tier, int count)
        {
            return _tableStorage.InsertOrReplaceAsync(TierUpgradeRequestEntity.CreateCount(tier, count));
        }

        public async Task<int> GetCountAsync(AccountTier tier)
        {
            var count = await _tableStorage.GetDataAsync(TierUpgradeRequestEntity.GenerateCountPk(tier),
                TierUpgradeRequestEntity.GenerateCountRk());

            return count?.Count ?? 0;
        }

        public async Task<Dictionary<string, int>> GetCountsAsync()
        {
            var result = new Dictionary<string, int>
            {
                { AccountTier.Apprentice.ToString(), 0 },
                { AccountTier.Advanced.ToString(), 0 },
                { AccountTier.ProIndividual.ToString(), 0 }
            };

            var partitionKeys = new List<string>
            {
                TierUpgradeRequestEntity.GenerateCountPk(AccountTier.Apprentice),
                TierUpgradeRequestEntity.GenerateCountPk(AccountTier.Advanced),
                TierUpgradeRequestEntity.GenerateCountPk(AccountTier.ProIndividual)
            };

            var counts = await _tableStorage.GetDataAsync(partitionKeys);

            foreach (var count in counts)
            {
                result[count.Tier.ToString()] = count.Count;
            }

            return result;
        }

        private string GetPendingRequestIndexRk(string clientId, AccountTier tier) => $"{clientId}_{tier}";
    }
}
