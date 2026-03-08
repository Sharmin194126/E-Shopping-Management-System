using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.ViewModels
{
    public class ProductTypeViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Product Type Name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(250)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [Display(Name = "Is Approved")]
        public bool IsApproved { get; set; }
    }
}
