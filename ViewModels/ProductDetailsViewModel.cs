using E_ShoppingManagement.Models;

namespace E_ShoppingManagement.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = new Product();
        public List<ReviewViewModel> Reviews { get; set; } = new List<ReviewViewModel>();
        public List<Product> RelatedProducts { get; set; } = new List<Product>();
        public Dictionary<int, int> RatingSummary { get; set; } = new Dictionary<int, int>();
        public List<ProductSizeStock> SizeStocks { get; set; } = new List<ProductSizeStock>();
        public int TotalReviews { get; set; }
    }
}
