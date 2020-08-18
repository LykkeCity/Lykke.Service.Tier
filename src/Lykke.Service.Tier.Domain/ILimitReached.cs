using System;

namespace Lykke.Service.Tier.Domain
{
    public interface ILimitReached
    {
        string ClientId { get; }
        double Amount { get; }
        double MaxAmount { get; }
        string Asset { get; }
        DateTime Date { get; }
    }
}
