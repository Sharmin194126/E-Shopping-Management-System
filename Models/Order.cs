using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_ShoppingManagement.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }     // map from UserId (Customer table)
        public Customer? Customer { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; } = "Pending"; // Pending / Processing / Delivered / Cancelled

        public string PaymentStatus { get; set; } = "Pending"; // Paid / Pending
        public string PaymentMethod { get; set; } = "COD"; // COD / Online

        public string? ShippingAddress { get; set; }
        public string? City { get; set; }
        public string? ZipCode { get; set; }
        public string? PhoneNumber { get; set; }

        public int? AssignedEmployeeId { get; set; }
        public Employee? AssignedEmployee { get; set; }

        public int? DeliveryManId { get; set; }
        public DeliveryMan? DeliveryMan { get; set; }

        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ModifiedAt { get; set; }

        public string Status { get; set; } = "Active"; // Active / Cancelled
        public string? ReturnReason { get; set; }

        public ICollection<OrderDetails>? OrderDetails { get; set; }
    }
}
