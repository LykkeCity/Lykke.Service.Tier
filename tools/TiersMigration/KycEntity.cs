using Microsoft.WindowsAzure.Storage.Table;

namespace TiersMigration
{
    public class KycEntity : TableEntity
    {
        internal const string DefaultStatus = "NeedToFillData";

        public string ClientId => RowKey;

        public string Status => PartitionKey;

        public string ProfileType { get; set; }

        public static string GeneratePartitionKey(string kycStatus)
        {
            return kycStatus;
        }

        public static string GenerateRowKey(string clientId)
        {
            return clientId;
        }
    }
}
