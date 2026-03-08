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

        public List<RecentOrderViewModel> RecentOrders { get; set; } = new List<RecentOrderViewModel>();
        public List<EmployeePerformanceViewModel> EmployeePerformances { get; set; } = new List<EmployeePerformanceViewModel>();
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
