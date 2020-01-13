using Microsoft.WindowsAzure.Storage.Table;

namespace TiersMigration
{
    public class KycDocumentEntity : TableEntity
    {
        public string ClientId => PartitionKey;

        public string Id => RowKey;

        public string Type { get; set; }
        public string State { get; set; }
    }
}
