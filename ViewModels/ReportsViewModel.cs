namespace E_ShoppingManagement.ViewModels
{
    public class ReportsViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public SummaryReportViewModel Summary { get; set; } = new();
        public List<DateWiseSalesViewModel> DateWiseSales { get; set; } = new();
        public List<EmployeeSalesViewModel> EmployeeSales { get; set; } = new();
        public List<PendingStockViewModel> PendingStock { get; set; } = new();
        public List<PendingDeliveryViewModel> PendingDelivery { get; set; } = new();
    }
}
