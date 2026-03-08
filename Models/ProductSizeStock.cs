using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class ProductSizeStock
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        public string Size { get; set; } = string.Empty; // L, M, XL, XXL, etc.
        public int StockQty { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    }
}
