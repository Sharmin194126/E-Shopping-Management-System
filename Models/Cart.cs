using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class Cart
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; } 

        public Customer? Customer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public ICollection<CartItem>? Items { get; set; }
    }
}
