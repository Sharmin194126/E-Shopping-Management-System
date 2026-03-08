namespace E_ShoppingManagement.ViewModels
{
    public class CartViewModel
    {
        public int CartId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.LineTotal);
    }
}
