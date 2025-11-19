using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class Cart : ITableEntity
    {
        [Required]
        public string PartitionKey { get; set; } = string.Empty;

        [Required]
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        [Required]
        public int UserId { get; set; }

        [Required]
        public string ProductId { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        public string ImageUrl { get; set; } = string.Empty;
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

        // Helper property (not stored in table)
        public decimal TotalPrice => Price * Quantity;
    }
}