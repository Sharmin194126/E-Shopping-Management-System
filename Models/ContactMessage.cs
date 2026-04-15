using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class ContactMessage
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
        public string? AttachmentUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Active";
        public string? Reply { get; set; }
    }
}
