using E_ShoppingManagement.Models;
using System.Collections.Generic;

namespace E_ShoppingManagement.ViewModels
{
    public class HomeIndexViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<ReviewViewModel> Reviews { get; set; } = new List<ReviewViewModel>();
        public FooterInfo? FooterInfo { get; set; }
        public IEnumerable<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
        public List<string> Banners { get; set; } = new List<string>();
        public List<DisplaySection> DisplaySections { get; set; } = new List<DisplaySection>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    }
}
