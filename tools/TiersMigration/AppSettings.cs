using System.Collections.Generic;

namespace TiersMigration
{
    public class AppSettings
    {
        public string KycStatusesConnString { get; set; }
        public string ClientPersonalInfoConnString { get; set; }
        public string TiersDataConnString { get; set; }
        public string PdApiKey { get; set; }
        public string PdServiceUrl { get; set; }
        public string TierServiceUrl { get; set; }
        public string ClintAccountServiceUrl { get; set; }
        public string TemplateFormatterUrl { get; set; }
        public string HistoryServiceUrl { get; set; }
        public string RateCalculatorServiceUrl { get; set; }
        public Dictionary<string, double> Tier2Emails { get; set; } = new Dictionary<string, double>();
    }
}
