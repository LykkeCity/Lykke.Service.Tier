using System;

namespace Lykke.Service.Tier.Domain.Deposits
{
    public interface IDepositOperation
    {
         string ClientId { get; }
         string FromClientId { get; }
         string OperationId { get; }
         string Asset { get; }
         double Amount { get; }
         string BaseAsset { get; }
         double BaseVolume { get; }
         string OperationType { get; }
         DateTime Date { get; }
    }
}
