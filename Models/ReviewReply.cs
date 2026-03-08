using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class ReviewReply
    {
        [Key]
        public int Id { get; set; }

        public int ReviewId { get; set; }
        public string UserId { get; set; } = string.Empty; // Seller/Admin/Employee
        public string ReplyText { get; set; } = string.Empty; // Was Reply
        public bool IsSeller { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Active";
    }
}
