using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_ShoppingManagement.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public CartSummaryViewComponent(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int itemCount = 0;
            decimal totalAmount = 0;

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                if (user != null)
                {
                    // Check if Customer
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
                    if (customer != null)
                    {
                        var cart = await _context.Carts
                            .Include(c => c.Items)
                            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && c.Status == "Active");

                        if (cart != null && cart.Items != null)
                        {
                            itemCount = cart.Items.Sum(i => i.Quantity);
                            totalAmount = cart.Items.Sum(i => i.Price * i.Quantity);
                        }
                    }
                    else
                    {
                    }
                }
            }

            ViewBag.TotalAmount = totalAmount;
            return View(itemCount);
        }
    }
}
