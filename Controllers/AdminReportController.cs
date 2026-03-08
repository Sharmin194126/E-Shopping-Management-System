using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using E_ShoppingManagement.Data;
using E_ShoppingManagement.ViewModels;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReportController : Controller
    {
        private readonly AppDbContext _context;

        public AdminReportController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            // Default last 30 days
            if (!fromDate.HasValue)
                fromDate = DateTime.UtcNow.Date.AddDays(-30);
            if (!toDate.HasValue)
                toDate = DateTime.UtcNow.Date;

            // Base query: only Active orders in date range
            var ordersQuery = _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.Customer)
                .Include(o => o.AssignedEmployee)
                .Where(o => o.Status == "Active" &&
                            o.CreatedAt.Date >= fromDate.Value.Date &&
                            o.CreatedAt.Date <= toDate.Value.Date);

            // Summary
            var summary = new SummaryReportViewModel
            {
                TotalOrders = await ordersQuery.CountAsync(),
                TotalRevenue = await ordersQuery.SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
                TotalProductsSold = await ordersQuery
                    .SelectMany(o => o.OrderDetails)
                    .SumAsync(od => (int?)od.Quantity) ?? 0
            };

            // Date-wise sales
            var dateWiseSales = await ordersQuery
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new DateWiseSalesViewModel
                {
                    Date = g.Key,
                    OrdersCount = g.Count(),
                    ProductsSold = g.SelectMany(o => o.OrderDetails).Sum(od => od.Quantity),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            // Employee-wise sales (only orders where AssignedEmployeeId is set)
            var employeeSales = await ordersQuery
                .Where(o => o.AssignedEmployeeId != null)
                .GroupBy(o => new { o.AssignedEmployeeId, o.AssignedEmployee!.Name })
                .Select(g => new EmployeeSalesViewModel
                {
                    EmployeeId = g.Key.AssignedEmployeeId ?? 0,
                    EmployeeName = g.Key.Name ?? "Unknown",
                    OrdersHandled = g.Count(),
                    ProductsSold = g.SelectMany(o => o.OrderDetails).Sum(od => od.Quantity),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(r => r.Revenue)
                .ToListAsync();

            // Pending stock & pending delivery use all active orders (not only date range)
            var pendingOrderDetails = _context.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.Order!.Status == "Active" &&
                             od.Order.OrderStatus == "Pending");

            // Pending stock: products where some quantity is in pending orders or low stock
            var pendingStock = await _context.Products
                .Select(p => new PendingStockViewModel
                {
                    ProductId = p.Id,
                    ProductName = p.Name ?? "",
                    StockQty = p.StockQty,
                    PendingOrderQty = pendingOrderDetails
                        .Where(od => od.ProductId == p.Id)
                        .Sum(od => (int?)od.Quantity) ?? 0
                })
                .Where(r => r.PendingOrderQty > 0 || r.StockQty <= 5) // threshold: 5
                .OrderBy(r => r.StockQty)
                .ToListAsync();

            // Pending delivery: orders not Delivered (across all)
            var pendingDelivery = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.Status == "Active" &&
                            o.OrderStatus != "Delivered")
                .Select(o => new PendingDeliveryViewModel
                {
                    OrderId = o.Id,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Unknown",
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus
                })
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            var vm = new ReportsViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Summary = summary,
                DateWiseSales = dateWiseSales,
                EmployeeSales = employeeSales,
                PendingStock = pendingStock,
                PendingDelivery = pendingDelivery
            };

            return View(vm);
        }
    }
}
