using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class ProductType
    {
        [Key]
        public int Id { get; set; }
        public string? ImageUrl { get; set; }

        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; } 
        public int? StockQty { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? ItemType { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }

        public bool IsApproved { get; set; } = false;

        public string? ApprovedBy { get; set; }
        public string? RejectedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RejectedAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Active";
    }
}
