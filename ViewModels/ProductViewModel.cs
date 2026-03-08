using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace E_ShoppingManagement.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string? Name { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required]
        [Display(Name = "Product Type")]
        public int ProductTypeId { get; set; }

        [Required]
        [Range(0, 999999999)]
        public decimal Price { get; set; }

        [Display(Name = "Regular Price")]
        public decimal RegularPrice { get; set; }
        
        [Display(Name = "VAT %")]
        public decimal VatPercentage { get; set; }

        [Display(Name = "Offer %")]
        public decimal OfferPercentage { get; set; }

        [Required]
        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue)]
        public int StockQty { get; set; }

        [Display(Name = "Max Order Limit")]
        [Range(1, 9999)]
        public int? MaxOrderQty { get; set; }

        public string? Description { get; set; }

        public string Status { get; set; } = "Pending";

        [Display(Name = "Approved")]
        public bool IsApproved { get; set; }

        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Display Section")]
        public string? DisplayCategory { get; set; } // Feature, Exclusive, Offer, JustForYou, Restock

        [Display(Name = "Available Sizes")]
        public string? AvailableSizes { get; set; } // L, M, XL, etc.

        [Display(Name = "Assign to Employee")]
        public int? AssignedEmployeeId { get; set; }

        public List<Models.ProductSizeStock>? SizeStocks { get; set; }

        // Dropdowns
        public IEnumerable<SelectListItem>? CategoryList { get; set; }
        public IEnumerable<SelectListItem>? ProductTypeList { get; set; }
        public IEnumerable<SelectListItem>? DisplayCategoryList { get; set; }
        public IEnumerable<SelectListItem>? StatusList { get; set; }
        public IEnumerable<SelectListItem>? EmployeeList { get; set; }

        // For filtering on Index
        public int? FilterCategoryId { get; set; }
        public int? FilterProductTypeId { get; set; }
    }
}
