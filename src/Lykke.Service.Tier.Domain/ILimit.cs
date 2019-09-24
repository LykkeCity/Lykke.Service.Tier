using System;

namespace Lykke.Service.Tier.Domain
{
    public interface ILimit
    {
        string ClientId { get; }
        double Limit { get; }
        string Asset { get; }
        DateTime Date { get; }
    }
}
