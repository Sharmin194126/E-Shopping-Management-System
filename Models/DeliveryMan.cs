using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class DeliveryMan
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public Users? User { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string ContactNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? VehicleInfo { get; set; }

        public string Status { get; set; } = "Active"; // Active / Inactive
        public decimal CommissionRate { get; set; } = 0;
        public decimal TotalEarnings { get; set; } = 0;
        public decimal PaidAmount { get; set; } = 0;
        public decimal PendingAmount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
