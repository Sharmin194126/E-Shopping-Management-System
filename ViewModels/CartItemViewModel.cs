using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.ViewModels
{
    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal VatAmount { get; set; }
        public decimal PriceWithVat { get; set; }

        public string? Size { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        public decimal LineTotal => PriceWithVat * Quantity;
    }
}
