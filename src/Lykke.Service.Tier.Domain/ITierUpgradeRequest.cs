using System;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Tier.Contract;

namespace Lykke.Service.Tier.Domain
{
    public interface ITierUpgradeRequest
    {
        string ClientId { get; }
        AccountTier Tier { get; }
        KycStatus KycStatus { get; }
        DateTime Date { get; }
        string Comment { get; }
        int Count { get; }
    }
}
