using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(15)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be exactly 11 digits.")]
        public string? PhoneNumber { get; set; }

        [StringLength(20)]
        public string Role { get; set; } = "Customer";

        public string? ProfilePictureUrl { get; set; }

        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";
    }
}
