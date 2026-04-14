using System.Collections.Generic;

namespace E_ShoppingManagement.ViewModels
{
    public class EmployeeStatsViewModel
    {
        public int TotalProductsManaged { get; set; }
        public int TotalInventoryQty { get; set; }
        public decimal TotalStockValue { get; set; }

        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int DeliveredOrders { get; set; }

        public decimal TotalSalesValue { get; set; }
        public int TotalDeliveryMen { get; set; }
        public int ActiveDeliveries { get; set; }

        public decimal SalesGoal { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal WeeklyRevenue { get; set; }
        public int CurrentYear { get; set; }
        public int PreviousYear { get; set; }

        public List<RecentOrderViewModel> AssignedOrders { get; set; } = new List<RecentOrderViewModel>();

        // Chart Data
        public List<DailySalesViewModel> DailySales { get; set; } = new List<DailySalesViewModel>();
        public List<DailySalesViewModel> WeeklySales { get; set; } = new List<DailySalesViewModel>();
        public List<ProductTypeSalesViewModel> ProductTypeSales { get; set; } = new List<ProductTypeSalesViewModel>();
        public List<MonthlySalesViewModel> MonthlyHistory { get; set; } = new List<MonthlySalesViewModel>();
        public List<MonthlySalesViewModel> PreviousYearSales { get; set; } = new List<MonthlySalesViewModel>();
        public List<ProductSalesSummaryViewModel> TopProducts { get; set; } = new List<ProductSalesSummaryViewModel>();
    }
}
