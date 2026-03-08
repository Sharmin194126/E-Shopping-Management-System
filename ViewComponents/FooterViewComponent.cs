using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_ShoppingManagement.ViewComponents
{
    public class FooterViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public FooterViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var footerInfo = await _context.FooterInfos.FirstOrDefaultAsync() 
                             ?? new FooterInfo { Address = "Updating...", ContactNumber = "000" };
            
            var paymentMethods = await _context.PaymentMethods
                                 .Where(p => p.Status == "Active")
                                 .ToListAsync();

            var reviews = await _context.Reviews
                            .OrderByDescending(r => r.CreatedAt)
                            .Take(3)
                            .ToListAsync();
            
            var reviewViewModels = new List<ViewModels.ReviewViewModel>();
            foreach(var r in reviews)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == r.UserId);
                reviewViewModels.Add(new ViewModels.ReviewViewModel {
                    CustomerName = customer?.Name ?? "Anonymous",
                    ProfilePicture = customer?.ProfilePictureUrl,
                    Rating = r.Rating,
                    Comment = r.Comment
                });
            }

            var vm = new FooterViewModel
            {
                Info = footerInfo,
                PaymentMethods = paymentMethods,
                RecentReviews = reviewViewModels
            };

            return View(vm);
        }
    }

    public class FooterViewModel
    {
        public FooterInfo Info { get; set; } = new();
        public List<PaymentMethod> PaymentMethods { get; set; } = new();
        public List<ViewModels.ReviewViewModel> RecentReviews { get; set; } = new();
    }
}
