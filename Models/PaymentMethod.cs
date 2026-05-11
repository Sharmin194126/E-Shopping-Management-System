using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class PaymentMethod
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty; // bkash, nogod, rocket, etc.

        public string? Details { get; set; } // instructions or usage details
        
        [StringLength(100)]
        public string? AccountNumber { get; set; } // The actual receiving number

        public string? LogoUrl { get; set; }

        public bool IsActive { get; set; } = true;
        
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
