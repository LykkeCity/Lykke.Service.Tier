namespace Lykke.Service.Tier.Client.Models.Requests
{
    public class SetLimitRequest
    {
        public string ClientId { get; set; }
        public double Limit { get; set; }
    }
}
