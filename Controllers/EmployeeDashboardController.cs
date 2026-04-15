using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using E_ShoppingManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeDashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly IWebHostEnvironment _env;

        public EmployeeDashboardController(AppDbContext context, UserManager<Users> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            user.LastNotificationCheck = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null) return NotFound();

            var orders = await _context.Orders
                .Where(o => o.AssignedEmployeeId == employee.Id)
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedAt)
                .Take(30)
                .ToListAsync();
            
            var messages = await _context.ContactMessages.OrderByDescending(m => m.CreatedAt).Take(20).ToListAsync();
            ViewBag.RecentMessages = messages;

            return View(orders);
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null) return NotFound();

            if (employee.Status != "Active")
            {
                return View("PendingApproval");
            }

            var stats = new EmployeeStatsViewModel();
            stats.EmployeeId = employee.Id;

            var products = await _context.Products.Where(p => p.CreatedBy == employee.Email || p.CreatedBy == employee.Name).ToListAsync();
            stats.TotalProductsManaged = products.Count;
            stats.TotalInventoryQty = products.Sum(p => p.StockQty);
            stats.TotalStockValue = products.Sum(p => p.Price * p.StockQty);

            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .Where(o => o.AssignedEmployeeId == employee.Id)
                .ToListAsync();

            stats.PendingOrders = orders.Count(o => o.OrderStatus == "Pending");
            stats.ProcessingOrders = orders.Count(o => o.OrderStatus == "Processing");
            stats.DeliveredOrders = orders.Count(o => o.OrderStatus == "Delivered");
            
            var soldOrders = orders
                .Where(o => o.OrderStatus == "Delivered" || o.PaymentStatus == "Paid" || o.OrderStatus == "Shipped")
                .ToList();

            stats.TotalSalesValue = soldOrders.Sum(o => o.TotalAmount);
            stats.TotalRevenue = stats.TotalSalesValue;
            stats.WeeklyRevenue = soldOrders.Where(o => o.CreatedAt.Date >= DateTime.UtcNow.Date.AddDays(-6)).Sum(o => o.TotalAmount);

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

            // Daily Sales
            for (int i = 0; i < 30; i++)
            {
                var date = DateTime.UtcNow.Date.AddDays(-29 + i);
                var dayO = soldOrders.Where(o => o.CreatedAt.Date == date).ToList();
                var dayIds = dayO.Select(o => o.Id).ToList();
                var pieces = await _context.OrderDetails.Where(od => dayIds.Contains(od.OrderId)).SumAsync(od => (int?)od.Quantity) ?? 0;
                stats.DailySales.Add(new DailySalesViewModel { Date = date, Amount = dayO.Sum(o => o.TotalAmount), Pieces = pieces });
            }

            // Weekly
            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.UtcNow.Date.AddDays(-6 + i);
                var dayO = soldOrders.Where(o => o.CreatedAt.Date == date).ToList();
                var dayIds = dayO.Select(o => o.Id).ToList();
                var pieces = await _context.OrderDetails.Where(od => dayIds.Contains(od.OrderId)).SumAsync(od => (int?)od.Quantity) ?? 0;
                stats.WeeklySales.Add(new DailySalesViewModel { Date = date, Amount = dayO.Sum(o => o.TotalAmount), Pieces = pieces });
            }

            // Monthly
            var monthlyGrouped = soldOrders
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(o => o.TotalAmount), OrderIds = g.Select(o => o.Id).ToList() })
                .OrderByDescending(g => g.Year).ThenByDescending(g => g.Month).ToList();

            foreach (var item in monthlyGrouped)
            {
                var pieces = await _context.OrderDetails.Where(od => item.OrderIds.Contains(od.OrderId)).SumAsync(od => (int?)od.Quantity) ?? 0;
                stats.MonthlyHistory.Add(new MonthlySalesViewModel { Year = item.Year, Month = item.Month, MonthName = new DateTime(item.Year, item.Month, 1).ToString("MMM"), Amount = item.Amount, Pieces = pieces });
            }

            stats.CurrentYear = DateTime.UtcNow.Year;
            stats.PreviousYear = stats.CurrentYear - 1;

            stats.CurrentYear = DateTime.UtcNow.Year;
            stats.PreviousYear = stats.CurrentYear - 1;

            string goalFolderPath = Path.Combine(_env.WebRootPath, "settings", "goals");
            if (!Directory.Exists(goalFolderPath)) Directory.CreateDirectory(goalFolderPath);

            string goalFilePath = Path.Combine(goalFolderPath, $"employee_goal_{employee.Id}_{stats.CurrentYear}.txt");
            if (System.IO.File.Exists(goalFilePath) && decimal.TryParse(System.IO.File.ReadAllText(goalFilePath), out decimal savedGoal))
            {
                stats.SalesGoal = savedGoal;
            }
            else
            {
                var lastYearRevenue = soldOrders.Where(o => o.CreatedAt.Year == stats.PreviousYear).Sum(o => o.TotalAmount);
                stats.SalesGoal = lastYearRevenue > 0 ? lastYearRevenue * 1.2m : (stats.TotalRevenue > 0 ? stats.TotalRevenue * 0.1m : 50000);
            }

            for (int m = 1; m <= 12; m++)
            {
                var monthOrders = soldOrders.Where(o => o.CreatedAt.Year == stats.PreviousYear && o.CreatedAt.Month == m).ToList();
                var mIds = monthOrders.Select(o => o.Id).ToList();
                var mPieces = mIds.Any() ? await _context.OrderDetails.Where(od => mIds.Contains(od.OrderId)).SumAsync(od => (int?)od.Quantity) ?? 0 : 0;
                stats.PreviousYearSales.Add(new MonthlySalesViewModel { Year = stats.PreviousYear, Month = m, MonthName = new DateTime(stats.PreviousYear, m, 1).ToString("MMM"), Amount = monthOrders.Sum(o => o.TotalAmount), Pieces = mPieces });
            }

            // Top Products
            var prodData = await _context.OrderDetails
                .Include(od => od.Product).ThenInclude(p => p.Category)
                .Include(od => od.Order)
                .Where(od => od.Order.AssignedEmployeeId == employee.Id && (od.Order.OrderStatus == "Delivered" || od.Order.PaymentStatus == "Paid" || od.Order.OrderStatus == "Shipped"))
                .GroupBy(od => new { od.Product.Id, od.Product.Name, CategoryName = od.Product.Category != null ? od.Product.Category.Name : "General" })
                .Select(g => new ProductSalesSummaryViewModel
                {
                    ProductId = g.Key.Id,
                    ProductName = g.Key.Name ?? "Unknown",
                    TotalQuantity = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.PriceWithVat * od.Quantity),
                    Category = g.Key.CategoryName
                })
                .OrderByDescending(p => p.TotalQuantity)
                .Take(6)
                .ToListAsync();
            stats.TopProducts = prodData;

            // Product Type Pie
            var pieData = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product).ThenInclude(p => p.ProductType)
                .Where(od => od.Order.AssignedEmployeeId == employee.Id && (od.Order.OrderStatus == "Delivered" || od.Order.PaymentStatus == "Paid" || od.Order.OrderStatus == "Shipped"))
                .GroupBy(od => od.Product.ProductType.Name)
                .Select(g => new ProductTypeSalesViewModel
                {
                    ProductTypeName = g.Key ?? "Unspecified",
                    Amount = g.Sum(od => od.PriceWithVat * od.Quantity),
                    Pieces = g.Sum(od => od.Quantity)
                })
                .ToListAsync();
            stats.ProductTypeSales = pieData;

            return View(stats);
        }

        [HttpGet]
        public async Task<IActionResult> GetYearlyGoalStats(int year)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null) return NotFound();

            var soldOrders = await _context.Orders
                .Where(o => o.AssignedEmployeeId == employee.Id && o.CreatedAt.Year == year && (o.OrderStatus == "Delivered" || o.PaymentStatus == "Paid" || o.OrderStatus == "Shipped"))
                .ToListAsync();
            
            decimal totalRevenue = soldOrders.Sum(o => o.TotalAmount);
            decimal goalAmount = 0;
            
            string goalFolderPath = Path.Combine(_env.WebRootPath, "settings", "goals");
            string goalFilePath = Path.Combine(goalFolderPath, $"employee_goal_{employee.Id}_{year}.txt");
            
            if (System.IO.File.Exists(goalFilePath) && decimal.TryParse(System.IO.File.ReadAllText(goalFilePath), out decimal savedGoal))
            {
                goalAmount = savedGoal;
            }
            else
            {
                // Fallback logic for employee
                var prevYearRevenue = await _context.Orders
                    .Where(o => o.AssignedEmployeeId == employee.Id && o.CreatedAt.Year == (year - 1) && (o.OrderStatus == "Delivered" || o.PaymentStatus == "Paid" || o.OrderStatus == "Shipped"))
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                goalAmount = prevYearRevenue > 0 ? prevYearRevenue * 1.2m : 50000;
            }

            return Json(new { 
                year = year,
                revenue = totalRevenue,
                goal = goalAmount,
                percent = goalAmount > 0 ? (int)Math.Min(100, (totalRevenue / goalAmount) * 100) : 0,
                remaining = goalAmount - totalRevenue
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetYearlySales(int year)
        {
            var user = await _userManager.GetUserAsync(User);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null) return Unauthorized();

            var monthlySales = new List<MonthlySalesViewModel>();
            var soldOrders = await _context.Orders
                .Where(o => o.AssignedEmployeeId == employee.Id && o.CreatedAt.Year == year && (o.OrderStatus == "Delivered" || o.PaymentStatus == "Paid" || o.OrderStatus == "Shipped"))
                .ToListAsync();

            for (int m = 1; m <= 12; m++)
            {
                var monthOrders = soldOrders.Where(o => o.CreatedAt.Month == m).ToList();
                var mIds = monthOrders.Select(o => o.Id).ToList();
                var mPieces = mIds.Any() ? await _context.OrderDetails.Where(od => mIds.Contains(od.OrderId)).SumAsync(od => (int?)od.Quantity) ?? 0 : 0;
                
                monthlySales.Add(new MonthlySalesViewModel
                {
                    Year = year,
                    Month = m,
                    MonthName = new DateTime(year, m, 1).ToString("MMM"),
                    Amount = monthOrders.Sum(o => o.TotalAmount),
                    Pieces = mPieces
                });
            }
            return Json(monthlySales);
        }

        public async Task<IActionResult> Messages()
        {
            var messages = await _context.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync();
            return View(messages);
        }

        public async Task<IActionResult> MessageDetails(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();
            return View(message);
        }

        [HttpPost]
        public async Task<IActionResult> ReplyMessage(int id, string reply)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            message.Reply = reply;
            message.Status = "Replied";
            await _context.SaveChangesAsync();

            TempData["Message"] = "Reply sent successfully!";
            return RedirectToAction(nameof(Messages));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSalesGoal(decimal goalAmount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null) return NotFound();

            int currentYear = DateTime.UtcNow.Year;
            string folderPath = Path.Combine(_env.WebRootPath, "settings", "goals");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            
            string filePath = Path.Combine(folderPath, $"employee_goal_{employee.Id}_{currentYear}.txt");
            System.IO.File.WriteAllText(filePath, goalAmount.ToString());
            
            TempData["Message"] = $"Sales goal for {currentYear} updated successfully!";
            TempData["IsSuccess"] = true;
            return RedirectToAction(nameof(Index));
        }
    }
}
