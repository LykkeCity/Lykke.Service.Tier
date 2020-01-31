using Microsoft.WindowsAzure.Storage.Table;

namespace TiersMigration
{
    public class PaymentEntity : TableEntity
    {
        public string ClientId => PartitionKey;

        public string OperationId => RowKey;

        public string PaymentSystem { get; set; }
    }
}
