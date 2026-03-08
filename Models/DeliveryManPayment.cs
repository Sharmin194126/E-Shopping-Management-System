using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class DeliveryManPayment
    {
        public int Id { get; set; }
        public int DeliveryManId { get; set; }
        public DeliveryMan? DeliveryMan { get; set; }
        
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        
        public decimal CommissionAmount { get; set; }
        public decimal OrderTotal { get; set; }
        public string Status { get; set; } = "Pending"; // Pending / Paid
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }
}
