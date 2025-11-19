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
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}