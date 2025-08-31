using Azure.Data.Tables;
using Azure;

namespace ABCRetailers.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string OrderId => RowKey;

        public string CustomerId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTimeOffset OrderDate { get; set; }
        public string? PaymentProofFileName { get; set; }

        public int Quantity { get; set; }

        // ✅ Changed from decimal to double
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }

        public string Status { get; set; } = "Submitted";
    }
}
