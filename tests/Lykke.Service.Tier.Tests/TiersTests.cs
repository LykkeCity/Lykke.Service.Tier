using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Tier.AzureRepositories;
using Lykke.Service.Tier.Contract;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Services;
using Lykke.Service.Tier.Domain.Settings;
using Lykke.Service.Tier.DomainServices;
using Moq;
using Xunit;

namespace Lykke.Service.Tier.Tests
{
    public class TiersTests
    {
        private readonly Mock<ILimitsService> _limitsService;
        private readonly Mock<ISettingsService> _settingsService;
        private readonly Mock<ITierUpgradeService> _tierUpgradeService;
        private readonly TiersService _tierService;
        private const string ClientId = "1";
        private const string HighRiskCountry = "High";
        private const string LowMidRiskCountry = "Low";

        public TiersTests()
        {
            _limitsService = new Mock<ILimitsService>();
            _settingsService = new Mock<ISettingsService>();
            _tierUpgradeService = new Mock<ITierUpgradeService>();

            _tierService = new TiersService(_limitsService.Object, _settingsService.Object, _tierUpgradeService.Object);
        }

        [Fact]
        public async Task TierInfo_JustRegisteredClient_Ntfd_LowRisk()
        {
            InitTest();
            var info = await _tierService.GetClientTierInfoAsync(ClientId, AccountTier.Beginner, LowMidRiskCountry);

            CheckResult(info, AccountTier.Beginner, AccountTier.Advanced);
        }

        [Fact]
        public async Task TierInfo_JustRegisteredClient_Ntfd_HighRisk()
        {
            InitTest();
            var info = await _tierService.GetClientTierInfoAsync(ClientId, AccountTier.Beginner, HighRiskCountry);

            CheckResult(info, AccountTier.Beginner, AccountTier.ProIndividual);
        }

        [Fact]
        public async Task TierInfo_JustRegisteredClient_Pending_SubmitedToApprentice_LowRisk()
        {
            var requests = new List<ITierUpgradeRequest>
            {
                new TierUpgradeRequestEntity
                {
                    Tier = AccountTier.Advanced,
                    KycStatus = KycStatus.Pending
                }
            };

            InitTest(requests);
            var info = await _tierService.GetClientTierInfoAsync(ClientId, AccountTier.Beginner, LowMidRiskCountry);

            CheckResult(info, AccountTier.Beginner, AccountTier.ProIndividual,
                AccountTier.Advanced, KycStatus.Pending.ToString());
        }

        [Fact]
        public async Task TierInfo_JustRegisteredClient_Pending_SubmitedToProIndividual_HighRisk()
        {
            var requests = new List<ITierUpgradeRequest>
            {
                new TierUpgradeRequestEntity
                {
                    Tier = AccountTier.ProIndividual,
                    KycStatus = KycStatus.Pending
                }
            };

            InitTest(requests);
            var info = await _tierService.GetClientTierInfoAsync(ClientId, AccountTier.Beginner, HighRiskCountry);

            CheckResult(info, AccountTier.Beginner, null, AccountTier.ProIndividual, KycStatus.Pending.ToString());
        }

        [Fact]
        public async Task TierInfo_KycOK_SubmitedToAdvanced_LowRisk()
        {
            var requests = new List<ITierUpgradeRequest>
            {
                new TierUpgradeRequestEntity
                {
                    Tier = AccountTier.Advanced,
                    KycStatus = KycStatus.Pending
                }
            };

            InitTest(requests);
            var info = await _tierService.GetClientTierInfoAsync(ClientId, AccountTier.Beginner, LowMidRiskCountry);

            CheckResult(info, AccountTier.Beginner, AccountTier.ProIndividual,
                AccountTier.Advanced, KycStatus.Pending.ToString());
        }

        [Fact]
        public async Task TierInfo_KycOK_SubmitedToAdvancedAndProIndividual_LowRisk()
        {
            var requests = new List<ITierUpgradeRequest>
            {
                new TierUpgradeRequestEntity
                {
                    Tier = AccountTier.Advanced,
                    KycStatus = KycStatus.Pending
                },
                new TierUpgradeRequestEntity
                {
                    Tier = AccountTier.ProIndividual,
                    KycStatus = KycStatus.Pending
                }
            };

            InitTest(requests);
            var info = await _tierService.GetClientTierInfoAsync(ClientId, AccountTier.Beginner, LowMidRiskCountry);

            CheckResult(info, AccountTier.Beginner, null,
                AccountTier.ProIndividual, KycStatus.Pending.ToString());
        }

        [Fact]
        public async Task TierInfo_KycOK_SubmitedToAdvancedAndProIndividual_AdvancedRejected_LowRisk()
        {
            var requests = new List<ITierUpgradeRequest>
            {
                new TierUpgradeRequestEntity
                {
                    Tier = AccountTier.Advanced,
                    KycStatus = KycStatus.Rejected
                },
                new TierUpgradeRequestEntity
                {
                    Tier = AccountTier.ProIndividual,
                    KycStatus = KycStatus.Pending
                }
            };

            InitTest(requests);
            var info = await _tierService.GetClientTierInfoAsync(ClientId, AccountTier.Beginner, LowMidRiskCountry);

            CheckResult(info, AccountTier.Beginner, AccountTier.Advanced,
                AccountTier.Advanced, KycStatus.Rejected.ToString());
        }

        private void InitTest(List<ITierUpgradeRequest> requests = null)
        {
            _tierUpgradeService.Setup(x => x.GetByClientAsync(It.IsAny<string>())).ReturnsAsync(requests ?? new List<ITierUpgradeRequest>());
            _settingsService.Setup(x => x.IsHighRiskCountry(HighRiskCountry)).Returns(true);
            _settingsService.Setup(x => x.IsHighRiskCountry(LowMidRiskCountry)).Returns(false);

            _limitsService.Setup(x => x.GetClientLimitSettingsAsync(It.IsAny<string>(), AccountTier.Advanced, LowMidRiskCountry))
                .ReturnsAsync(new LimitSettings
                {
                    Tier = AccountTier.Advanced,
                    Documents = new List<DocumentType>{ DocumentType.PoA },
                    MaxLimit = 15000
                });

            _limitsService.Setup(x => x.GetClientLimitSettingsAsync(It.IsAny<string>(), AccountTier.ProIndividual, LowMidRiskCountry))
                .ReturnsAsync(new LimitSettings
                {
                    Tier = AccountTier.ProIndividual,
                    Documents = new List<DocumentType>{ DocumentType.PoF },
                    MaxLimit = 15000
                });

            _limitsService.Setup(x => x.GetClientLimitSettingsAsync(It.IsAny<string>(), It.IsAny<AccountTier>(), HighRiskCountry))
                .ReturnsAsync((string clientId, AccountTier tier, string country) => new LimitSettings
                {
                    Tier = tier,
                    Documents = new List<DocumentType>{ DocumentType.PoI, DocumentType.Selfie },
                    MaxLimit = 500
                });
        }

        private void CheckResult(ClientTierInfo info, AccountTier currentTier, AccountTier? nextTier = null, AccountTier? upgradeRequestTier = null, string upgradeRequestStatus = null)
        {
            Assert.Equal(currentTier, info.CurrentTier.Tier);
            Assert.Equal(nextTier, info.NextTier?.Tier);
            Assert.Equal(upgradeRequestTier, info.UpgradeRequest?.Tier);
            Assert.Equal(upgradeRequestStatus, info.UpgradeRequest?.Status);
        }
    }
}
