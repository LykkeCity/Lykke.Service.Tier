using System;

namespace Lykke.Service.Tier.Domain
{
    public interface ILimit
    {
        string ClientId { get; }
        double Limit { get; }
        DateTime Date { get; }
    }
}
