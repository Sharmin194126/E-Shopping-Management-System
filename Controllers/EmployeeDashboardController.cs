using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using E_ShoppingManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeDashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public EmployeeDashboardController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null) return NotFound();

            var stats = new EmployeeStatsViewModel();

            // Products created by this employee
            var products = await _context.Products.Where(p => p.CreatedBy == employee.Email).ToListAsync();
            stats.TotalProductsManaged = products.Count;
            stats.TotalInventoryQty = products.Sum(p => p.StockQty);
            stats.TotalStockValue = products.Sum(p => p.Price * p.StockQty);

            // Orders assigned to this employee
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .Where(o => o.AssignedEmployeeId == employee.Id)
                .ToListAsync();

            stats.PendingOrders = orders.Count(o => o.OrderStatus == "Pending");
            stats.ProcessingOrders = orders.Count(o => o.OrderStatus == "Processing");
            stats.DeliveredOrders = orders.Count(o => o.OrderStatus == "Delivered");
            stats.TotalSalesValue = orders.Where(o => o.OrderStatus == "Delivered").Sum(o => o.TotalAmount);

            // Delivery stats
            stats.TotalDeliveryMen = await _context.DeliveryMen.CountAsync(d => d.Status == "Active");
            stats.ActiveDeliveries = await _context.Orders.CountAsync(o => o.OrderStatus == "Shipping");

            stats.AssignedOrders = orders.OrderByDescending(o => o.CreatedAt).Take(10).Select(o => new RecentOrderViewModel
            {
                OrderId = o.Id,
                CustomerName = o.Customer?.Name ?? "Unknown",
                OrderDate = o.CreatedAt,
                Status = o.OrderStatus,
                Amount = o.TotalAmount,
                PaymentStatus = o.PaymentStatus,
                PaymentMethod = o.PaymentMethod,
                ShippingAddress = o.ShippingAddress ?? "N/A",
                ImageUrl = o.OrderDetails?.FirstOrDefault()?.Product?.ImageUrl ?? ""
            }).ToList();

            return View(stats);
        }
    }
}
