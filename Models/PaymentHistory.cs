using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class PaymentHistory
    {
        public int Id { get; set; }
        
        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(100)]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string GatewayName { get; set; } = string.Empty; // bKash, SSLCommerz, etc.

        public string? CustomerName { get; set; }
        public string? CustomerAccount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Success, Failed, Cancelled

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        
        public string? ResponsePayload { get; set; } // Detailed gateway response
    }
}
