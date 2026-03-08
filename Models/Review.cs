using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        public string UserId { get; set; } = string.Empty;

        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string? ImageUrls { get; set; } // Comma-separated image URLs

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Active";
    }
}
