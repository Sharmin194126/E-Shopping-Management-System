namespace E_ShoppingManagement.ViewModels
{
    public class EmployeeSalesViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int OrdersHandled { get; set; }
        public int ProductsSold { get; set; }
        public decimal Revenue { get; set; }
    }
}
