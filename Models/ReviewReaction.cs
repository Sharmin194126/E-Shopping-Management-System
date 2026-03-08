using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class ReviewReaction
    {
        [Key]
        public int Id { get; set; }

        public int ReviewId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string ReactionType { get; set; } = "Like"; // Like, Helpful, Love, etc.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
