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

        public List<RecentOrderViewModel> AssignedOrders { get; set; } = new List<RecentOrderViewModel>();
    }
}
