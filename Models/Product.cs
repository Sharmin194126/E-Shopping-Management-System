using System.ComponentModel.DataAnnotations;

namespace E_ShoppingManagement.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }
        public Category? Category { get; set; }


        public int CategoryId { get; set; }

        public ProductType? ProductType { get; set; }
        public int ProductTypeId { get; set; }

        public decimal Price { get; set; } // This will be the Offer Price
        public decimal RegularPrice { get; set; } // Renamed from PreviousPrice
        public int StockQty { get; set; }

        public string? ImageUrl { get; set; }
        public string? Description { get; set; }

        public string? CreatedBy { get; set; } // Employee
        public string? ModifiedBy { get; set; }

        public bool IsApproved { get; set; } = false;

        public string? ApprovedBy { get; set; }
        public string? RejectedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RejectedAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Pending"; // Changed from Active to Pending

        // New properties for UI display distribution
        public string? DisplayCategory { get; set; } // Feature, Exclusive, Offer, JustForYou, Restock
        public string? AvailableSizes { get; set; } // L, M, XL, etc.
        public double AverageRating { get; set; } = 0.0;
        public decimal VatPercentage { get; set; } = 0;
        public decimal OfferPercentage { get; set; } = 0;
        public int? MaxOrderQty { get; set; }

        // Relationship to Employee
        public int? AssignedEmployeeId { get; set; }
        public Employee? AssignedEmployee { get; set; }
    }
}
