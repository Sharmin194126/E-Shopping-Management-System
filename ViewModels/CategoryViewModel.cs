using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(300)]
        public string Description { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";
    }
}
