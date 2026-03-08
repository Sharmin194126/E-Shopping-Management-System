using E_ShoppingManagement.Models;

namespace E_ShoppingManagement.ViewModels
{
    public class CategoryProductsViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<ProductType> ProductTypes { get; set; } = new();
        
        // Filter selection
        public int? SelectedCategory { get; set; }
        public int? SelectedType { get; set; }
        public string? SelectedSize { get; set; }
        public string? PriceRange { get; set; }
        public string? SearchQuery { get; set; }
    }
}
