using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Tier.Client.Models;

namespace Lykke.Service.Tier.Extensions
{
    public static class TierModelExt
    {
        public static AccountTier ToAccountTier(this TierModel model)
        {
            return model == TierModel.Advanced
                ? AccountTier.Advanced
                : AccountTier.ProIndividual;
        }
    }
}
