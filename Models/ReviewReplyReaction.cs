using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class ReviewReplyReaction
    {
        [Key]
        public int Id { get; set; }

        public int ReviewReplyId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string ReactionType { get; set; } = "Like"; // Like, Helpful, etc.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
