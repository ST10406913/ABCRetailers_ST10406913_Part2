using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class FileUploadModel
    {
        [Required(ErrorMessage = "Please select a file")]
        public required IFormFile File { get; set; }

        public required string Description { get; set; }

        [Required(ErrorMessage = "Please select storage type")]
        public string StorageType { get; set; } = "blob"; // "blob" or "file"
    }
}