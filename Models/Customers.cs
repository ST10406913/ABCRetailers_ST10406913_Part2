using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Models
{
    public class Customers : ITableEntity
    {
        [Required]
        public string PartitionKey { get; set; } = "Customers";

        [Required]
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public required string Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public required string Phone { get; set; }

        public required string Address { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}