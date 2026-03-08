using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class OrderDetails
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal VatAmount { get; set; }
        public decimal VatPercentage { get; set; }
        public decimal PriceWithVat { get; set; }
        public decimal OfferPercentage { get; set; }
        public string? Size { get; set; }
    }
}
