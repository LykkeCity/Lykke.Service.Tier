using System;
using System.Collections.Generic;

namespace TiersMigration
{
    public class LimitationsResponse
    {
        public List<CashOperationResponse> CashOperations { get; set; }
        public List<CashOperationResponse> CashTransferOperations { get; set; }
    }

    public class CashOperationResponse
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public decimal Volume { get; set; }
        public string Asset { get; set; }
        public string OperationType { get; set; }
        public DateTime DateTime { get; set; }
    }
}
