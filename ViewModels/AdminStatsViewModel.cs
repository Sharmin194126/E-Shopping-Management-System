using System;
using System.Collections.Generic;

namespace E_ShoppingManagement.ViewModels
{
    public class AdminStatsViewModel
    {
        public int TotalDelivered { get; set; }
        public int TotalPending { get; set; }
        public int TotalProcessing { get; set; }
        public int TotalCancelled { get; set; }
        public int TotalShipped { get; set; }

        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public int PaidOrders { get; set; }
        public int PendingPaymentOrders { get; set; }

        public decimal TotalProfit { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal SalesGoal { get; set; }
        public decimal WeeklyRevenue { get; set; }

        public List<RecentOrderViewModel> RecentOrders { get; set; } = new List<RecentOrderViewModel>();
        public List<EmployeePerformanceViewModel> EmployeePerformances { get; set; } = new List<EmployeePerformanceViewModel>();

        // Chart Data
        public List<DailySalesViewModel> DailySales { get; set; } = new List<DailySalesViewModel>();
        public List<DailySalesViewModel> WeeklySales { get; set; } = new List<DailySalesViewModel>();
        public List<ProductTypeSalesViewModel> ProductTypeSales { get; set; } = new List<ProductTypeSalesViewModel>();
        public List<MonthlySalesViewModel> MonthlyHistory { get; set; } = new List<MonthlySalesViewModel>();
        public List<ProductSalesSummaryViewModel> TopProducts { get; set; } = new List<ProductSalesSummaryViewModel>();
        public List<MonthlySalesViewModel> PreviousYearSales { get; set; } = new List<MonthlySalesViewModel>();
        public int CurrentYear { get; set; }
        public int PreviousYear { get; set; }
    }

    public class ProductSalesSummaryViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public string? Category { get; set; }
    }

    public class DailySalesViewModel
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int Pieces { get; set; }
    }

    public class ProductTypeSalesViewModel
    {
        public string ProductTypeName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Pieces { get; set; }
    }

    public class MonthlySalesViewModel
    {
        public string MonthName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
        public int Pieces { get; set; }
    }

    public class RecentOrderViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
    }

    public class EmployeePerformanceViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int TotalProductsManaged { get; set; }
        public decimal TotalStockValue { get; set; }
        public int ProductsSold { get; set; }
        public decimal TotalSalesValue { get; set; }
        public List<Models.Product> ManagedProducts { get; set; } = new List<Models.Product>();
    }
}
