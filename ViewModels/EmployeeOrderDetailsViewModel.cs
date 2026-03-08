namespace E_ShoppingManagement.ViewModels
{
    public class EmployeeOrderDetailsViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<OrderDetailRow> Items { get; set; } = new();
    }
}
