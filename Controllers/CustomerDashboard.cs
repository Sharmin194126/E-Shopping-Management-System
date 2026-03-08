using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerDashboard : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public CustomerDashboard(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null)
            {
                 // Fallback if customer record not found but user exists
                 return View("Error"); 
            }

            var recentOrders = await _context.Orders
                .Where(o => o.CustomerId == customer.Id)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Include(o => o.OrderDetails)
                .ToListAsync();

            ViewBag.CustomerName = customer.Name;
            ViewBag.ProfilePictureUrl = customer.ProfilePictureUrl;
            
            return View(recentOrders);
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null) return NotFound();

            ViewBag.CustomerName = customer.Name;
            return View(customer);
        }

        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) return RedirectToAction("Index");

            var orders = await _context.Orders
                .Where(o => o.CustomerId == customer.Id)
                .OrderByDescending(o => o.CreatedAt)
                .Include(o => o.OrderDetails)
                .ToListAsync();

            ViewBag.CustomerName = customer.Name;
            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customer.Id);

            if (order == null) return NotFound();

            ViewBag.CustomerName = customer?.Name;
            return View(order);
        }

        public async Task<IActionResult> MoneyReceipt(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customer.Id);

            if (order == null) return NotFound();
            
            // Only allow if paid (as per user request: "when they paid on online")
            // But usually we show receipt whenever payment is done.
            if (order.PaymentStatus != "Paid")
            {
                TempData["Message"] = "Money receipt is only available for paid orders.";
                return RedirectToAction(nameof(OrderDetails), new { id = id });
            }

            ViewBag.CompanyInfo = await _context.FooterInfos.FirstOrDefaultAsync();
            return View(order);
        }
    }
}
