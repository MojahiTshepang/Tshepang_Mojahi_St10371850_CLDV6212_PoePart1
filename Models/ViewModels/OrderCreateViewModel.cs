using System.ComponentModel.DataAnnotations;
using ABCRetailers.Models;

namespace ABCRetailers.Models.ViewModels
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
        public int Quantity { get; set; }

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Submitted";

        // Dropdowns for selection in the view
        public List<Customer> Customers { get; set; } = new();
        public List<Product> Products { get; set; } = new();
    }
}
