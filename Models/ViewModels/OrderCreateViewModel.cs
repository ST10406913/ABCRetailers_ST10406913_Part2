using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ABCRetailers.Models;

namespace ABCRetailers.ViewModels
{
    public class OrderCreateViewModel
    {
        [Required]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product")]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Submitted";

        // FIXED: Use SelectListItem for dropdowns
        public List<SelectListItem> CustomerOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ProductOptions { get; set; } = new List<SelectListItem>();
    }
}