using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class FooterInfo
    {
        public int Id { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string ContactNumber { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? TwitterUrl { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
