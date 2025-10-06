using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Models
{
    public class Orders : ITableEntity
    {
        [Required]
        public string PartitionKey { get; set; } = "Orders";

        [Required]
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Required(ErrorMessage = "Customer ID is required")]
        public required string CustomerId { get; set; }

        public required string CustomerName { get; set; }

        [Required(ErrorMessage = "Product ID is required")]
        public required string ProductId { get; set; }

        public required string ProductName { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Total price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total price must be greater than 0")]
        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
    }

    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";
    }
}