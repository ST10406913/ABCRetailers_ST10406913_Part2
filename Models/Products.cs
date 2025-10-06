using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Models
{
    public class Products : ITableEntity
    {
        [Required]
        public string PartitionKey { get; set; } = "Products";

        [Required]
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public required string Category { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; }

        public required string ImageUrl { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}