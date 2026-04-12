using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using E_ShoppingManagement.ViewModels;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeOrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public EmployeeOrderController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Employee?> GetCurrentEmployeeAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
        }

        // Manage all orders (for employee, maybe only active ones)
        public async Task<IActionResult> Index(string status, string query)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.Status == "Active");

            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.OrderStatus == status);
            }

            if (!string.IsNullOrEmpty(query))
            {
                ordersQuery = ordersQuery.Where(o => o.Id.ToString().Contains(query) || (o.Customer != null && o.Customer.Name.Contains(query)));
            }

            var list = await ordersQuery
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new EmployeeOrderListViewModel
                {
                    OrderId = o.Id,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Unknown",
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            return View(list);
        }

        // View pending orders only
        public async Task<IActionResult> Pending()
        {
            var list = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.Status == "Active" && o.OrderStatus == "Pending")
                .OrderBy(o => o.CreatedAt)
                .Select(o => new EmployeeOrderListViewModel
                {
                    OrderId = o.Id,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Unknown",
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return View(list);
        }

        // View own sales count
        public async Task<IActionResult> MySales()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return Unauthorized();

            var orders = _context.Orders
                .Where(o => o.AssignedEmployeeId == employee.Id && o.Status == "Active");

            var totalOrders = await orders.CountAsync();
            var totalRevenue = await orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            var totalProducts = await orders.SelectMany(o => o.OrderDetails)
                .SumAsync(od => (int?)od.Quantity) ?? 0;

            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalProducts = totalProducts;

            var list = await orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new EmployeeOrderListViewModel
                {
                    OrderId = o.Id,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Unknown",
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    CreatedAt = o.CreatedAt,
                    Items = o.OrderDetails != null ? o.OrderDetails.Select(od => new OrderDetailRow
                    {
                        ProductName = od.Product != null ? od.Product.Name : "Unknown",
                        ProductImageUrl = od.Product != null ? od.Product.ImageUrl : null,
                        Quantity = od.Quantity,
                        Price = od.Price
                    }).ToList() : new List<OrderDetailRow>()
                })
                .ToListAsync();

            return View(list);
        }

        // Order details
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)!
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var vm = new EmployeeOrderDetailsViewModel
            {
                OrderId = order.Id,
                CustomerName = order.Customer != null ? order.Customer.Name : "Unknown",
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                CreatedAt = order.CreatedAt,
                Items = order.OrderDetails?.Select(od => new OrderDetailRow
                {
                    ProductName = od.Product != null ? od.Product.Name : "Unknown",
                    ProductImageUrl = od.Product != null ? od.Product.ImageUrl : null,
                    Quantity = od.Quantity,
                    Price = od.Price
                }).ToList() ?? new List<OrderDetailRow>()
            };

            return View(vm);
        }

        // Update order status (Processing / Shipped / Delivered)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var allowed = new[] { "Pending", "Processing", "Shipped", "Delivered" };
            if (!allowed.Contains(newStatus))
            {
                TempData["Message"] = "Invalid status.";
                TempData["IsSuccess"] = false;
                return RedirectToAction(nameof(Details), new { id });
            }

            if (order.AssignedEmployeeId == null)
            {
                var employee = await GetCurrentEmployeeAsync();
                if (employee != null)
                {
                    order.AssignedEmployeeId = employee.Id;
                }
            }

            order.OrderStatus = newStatus;
            order.ModifiedAt = DateTime.UtcNow;
            order.ModifiedBy = User.Identity?.Name;

            if (newStatus == "Processing") order.ProcessedAt = DateTime.UtcNow;
            if (newStatus == "Shipped") order.ShippedAt = DateTime.UtcNow;
            if (newStatus == "Delivered") order.DeliveredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Order status updated.";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

