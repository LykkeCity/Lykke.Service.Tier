using JetBrains.Annotations;

namespace Lykke.Service.Tier.Client
{
    /// <summary>
    /// Tier client interface.
    /// </summary>
    [PublicAPI]
    public interface ITierClient
    {
        /// <summary>Api for countries</summary>
        ICountriesApi Countries { get; }
    }
}
