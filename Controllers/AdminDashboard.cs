using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using E_ShoppingManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class AdminDashboard : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public AdminDashboard(IWebHostEnvironment env, AppDbContext context, UserManager<Users> userManager)
        {
            _env = env;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Real data for notifications
            var recentOrders = await _context.Orders.Include(o => o.Customer).OrderByDescending(o => o.CreatedAt).Take(20).ToListAsync();
            var recentCustomers = await _context.Customers.OrderByDescending(o => o.CreatedAt).Take(10).ToListAsync();
            var recentEmployees = await _context.Employees.OrderByDescending(o => o.CreatedAt).Take(10).ToListAsync();
            var recentMessages = await _context.ContactMessages.OrderByDescending(o => o.CreatedAt).Take(20).ToListAsync();

            ViewBag.RecentOrders = recentOrders;
            ViewBag.RecentCustomers = recentCustomers;
            ViewBag.RecentEmployees = recentEmployees;
            ViewBag.RecentMessages = recentMessages;

            return View();
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            return View(user);
        }

        public async Task<IActionResult> Index()
        {
            var stats = new AdminStatsViewModel();

            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .ToListAsync();
            stats.TotalOrders = orders.Count;
            stats.TotalDelivered = orders.Count(o => o.OrderStatus == "Delivered");
            stats.TotalPending = orders.Count(o => o.OrderStatus == "Pending");
            stats.TotalProcessing = orders.Count(o => o.OrderStatus == "Processing");
            stats.TotalShipped = orders.Count(o => o.OrderStatus == "Shipped");
            stats.TotalCancelled = orders.Count(o => o.OrderStatus == "Cancelled");
            stats.TotalRevenue = orders.Where(o => o.OrderStatus == "Delivered").Sum(o => o.TotalAmount);

            stats.PaidOrders = orders.Count(o => o.PaymentStatus == "Paid");
            stats.PendingPaymentOrders = orders.Count(o => o.PaymentStatus == "Pending");

            stats.RecentOrders = orders.OrderByDescending(o => o.CreatedAt).Take(10).Select(o => new RecentOrderViewModel
            {
                OrderId = o.Id,
                CustomerName = o.Customer?.Name ?? "Unknown",
                OrderDate = o.CreatedAt,
                Status = o.OrderStatus,
                Amount = o.TotalAmount,
                PaymentStatus = o.PaymentStatus,
                PaymentMethod = o.PaymentMethod,
                ShippingAddress = $"{o.ShippingAddress}, {o.City}",
                ImageUrl = o.OrderDetails?.FirstOrDefault()?.Product?.ImageUrl ?? ""
            }).ToList();

            // Employee Performance
            var employees = await _context.Employees.ToListAsync();
            foreach (var emp in employees)
            {
                var productsManaged = await _context.Products.Where(p => p.CreatedBy == emp.Email || p.CreatedBy == emp.Name).ToListAsync();
                
                // For sales, we need to check orders assigned to this employee
                var ordersAssigned = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.AssignedEmployeeId == emp.Id && o.OrderStatus == "Delivered")
                    .ToListAsync();

                stats.EmployeePerformances.Add(new EmployeePerformanceViewModel
                {
                    EmployeeId = emp.Id,
                    EmployeeName = emp.Name,
                    TotalProductsManaged = productsManaged.Count,
                    TotalStockValue = productsManaged.Sum(p => p.Price * p.StockQty),
                    ProductsSold = ordersAssigned.SelectMany(o => o.OrderDetails ?? new List<OrderDetails>()).Sum(od => od.Quantity),
                    TotalSalesValue = ordersAssigned.Sum(o => o.TotalAmount),
                    ManagedProducts = productsManaged // Added for details
                });
            }

            // Unified "Sold" definition: Delivered OR Paid OR Shipped
            var soldOrders = orders
                .Where(o => o.OrderStatus == "Delivered" || o.PaymentStatus == "Paid" || o.OrderStatus == "Shipped")
                .ToList();

            stats.TotalRevenue = soldOrders.Sum(o => o.TotalAmount);
            stats.WeeklyRevenue = soldOrders.Where(o => o.CreatedAt.Date >= DateTime.UtcNow.Date.AddDays(-6)).Sum(o => o.TotalAmount);
            // Profit approximation: Revenue minus VAT collected (VAT is an expense)
            var totalVatCollected = await _context.OrderDetails
                .Where(od => od.Order.OrderStatus == "Delivered" || od.Order.PaymentStatus == "Paid" || od.Order.OrderStatus == "Shipped")
                .SumAsync(od => od.VatAmount * od.Quantity);
            stats.TotalProfit = stats.TotalRevenue - totalVatCollected;
            stats.TotalExpenses = totalVatCollected;
            // SalesGoal is calculated below after PreviousYear is set

            // 1. Daily Sales (Last 30 Days)
            var last30Days = DateTime.UtcNow.Date.AddDays(-29);
            for (int i = 0; i < 30; i++)
            {
                var date = last30Days.AddDays(i);
                var dayOrders = soldOrders.Where(o => o.CreatedAt.Date == date).ToList();
                var dayIds = dayOrders.Select(o => o.Id).ToList();
                var dayPieces = await _context.OrderDetails.Where(od => dayIds.Contains(od.OrderId)).SumAsync(od => (int?)od.Quantity) ?? 0;
                stats.DailySales.Add(new DailySalesViewModel
                {
                    Date = date,
                    Amount = dayOrders.Sum(o => o.TotalAmount),
                    Pieces = dayPieces
                });
            }

            // 1.1 Weekly Sales (Last 7 Days)
            var last7Days = DateTime.UtcNow.Date.AddDays(-6);
            for (int i = 0; i < 7; i++)
            {
                var date = last7Days.AddDays(i);
                var dayOrders = soldOrders.Where(o => o.CreatedAt.Date == date).ToList();
                var dayIds = dayOrders.Select(o => o.Id).ToList();
                var dayPieces = await _context.OrderDetails.Where(od => dayIds.Contains(od.OrderId)).SumAsync(od => (int?)od.Quantity) ?? 0;
                stats.WeeklySales.Add(new DailySalesViewModel
                {
                    Date = date,
                    Amount = dayOrders.Sum(o => o.TotalAmount),
                    Pieces = dayPieces
                });
            }

            // 2. Product Type Distribution (real data)
            var productTypeData = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product).ThenInclude(p => p.ProductType)
                .Where(od => od.Order.OrderStatus == "Delivered" || od.Order.PaymentStatus == "Paid" || od.Order.OrderStatus == "Shipped")
                .GroupBy(od => od.Product.ProductType.Name)
                .Select(g => new ProductTypeSalesViewModel
                {
                    ProductTypeName = g.Key ?? "Unspecified",
                    Amount = g.Sum(od => od.PriceWithVat * od.Quantity),
                    Pieces = g.Sum(od => od.Quantity)
                })
                .ToListAsync();
            stats.ProductTypeSales = productTypeData;

            // 2.1 Top Selling Products (real data, with ProductId for navigation)
            var topRaw = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product).ThenInclude(p => p.Category)
                .Where(od => od.Order.OrderStatus == "Delivered" || od.Order.PaymentStatus == "Paid" || od.Order.OrderStatus == "Shipped")
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
                .Take(10)
                .ToListAsync();
            stats.TopProducts = topRaw;

            // 3. Monthly History (ALL historical records, newest first)
            var monthlyGrouped = soldOrders
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(o => o.TotalAmount),
                    OrderIds = g.Select(o => o.Id).ToList()
                })
                .OrderByDescending(g => g.Year).ThenByDescending(g => g.Month)
                .ToList();

            foreach (var item in monthlyGrouped)
            {
                var pieces = await _context.OrderDetails.Where(od => item.OrderIds.Contains(od.OrderId)).SumAsync(od => (int?)od.Quantity) ?? 0;
                stats.MonthlyHistory.Add(new MonthlySalesViewModel
                {
                    Year = item.Year,
                    Month = item.Month,
                    MonthName = new DateTime(item.Year, item.Month, 1).ToString("MMMM"),
                    Amount = item.Amount,
                    Pieces = pieces
                });
            }

            stats.CurrentYear = DateTime.UtcNow.Year;
            stats.PreviousYear = DateTime.UtcNow.Year - 1;

            // Fix SalesGoal: load from custom settings file if available
            string goalFilePath = System.IO.Path.Combine(_env.WebRootPath, "settings", "sales_goal.txt");
            if (System.IO.File.Exists(goalFilePath) && decimal.TryParse(System.IO.File.ReadAllText(goalFilePath), out decimal savedGoal))
            {
                stats.SalesGoal = savedGoal;
            }
            else
            {
                var lastYearRevenue = soldOrders.Where(o => o.CreatedAt.Year == stats.PreviousYear).Sum(o => o.TotalAmount);
                stats.SalesGoal = lastYearRevenue > 0 ? lastYearRevenue * 1.2m : stats.TotalRevenue * 1.2m;
                if (stats.SalesGoal == 0) stats.SalesGoal = 100000;
            }

            // Previous Year monthly sales (all 12 months)
            for (int m = 1; m <= 12; m++)
            {
                var monthOrders = soldOrders.Where(o => o.CreatedAt.Year == stats.PreviousYear && o.CreatedAt.Month == m).ToList();
                var mIds = monthOrders.Select(o => o.Id).ToList();
                var mPieces = mIds.Any() ? await _context.OrderDetails.Where(od => mIds.Contains(od.OrderId)).SumAsync(od => (int?)od.Quantity) ?? 0 : 0;
                stats.PreviousYearSales.Add(new MonthlySalesViewModel
                {
                    Year = stats.PreviousYear,
                    Month = m,
                    MonthName = new DateTime(stats.PreviousYear, m, 1).ToString("MMM"),
                    Amount = monthOrders.Sum(o => o.TotalAmount),
                    Pieces = mPieces
                });
            }

            return View(stats);

        }

        [HttpPost]
        public IActionResult UpdateSalesGoal(decimal goalAmount)
        {
            string folderPath = System.IO.Path.Combine(_env.WebRootPath, "settings");
            if (!System.IO.Directory.Exists(folderPath)) System.IO.Directory.CreateDirectory(folderPath);
            
            string filePath = System.IO.Path.Combine(folderPath, "sales_goal.txt");
            System.IO.File.WriteAllText(filePath, goalAmount.ToString());
            
            TempData["Message"] = "Sales goal updated successfully!";
            TempData["IsSuccess"] = true;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Shows all order details for a specific product (Top Products drill-down)</summary>
        public async Task<IActionResult> ProductSalesDetail(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductType)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            var orderDetails = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order).ThenInclude(o => o.Customer)
                .Where(od => od.ProductId == productId)
                .OrderByDescending(od => od.Order.CreatedAt)
                .ToListAsync();

            ViewBag.ProductName  = product.Name;
            ViewBag.ProductCategory = product.Category?.Name ?? "General";
            ViewBag.ProductType  = product.ProductType?.Name ?? "";
            ViewBag.TotalQty     = orderDetails.Sum(od => od.Quantity);
            ViewBag.TotalRevenue = orderDetails.Sum(od => od.PriceWithVat * od.Quantity);
            ViewBag.TotalVat     = orderDetails.Sum(od => od.VatAmount * od.Quantity);
            ViewBag.ProductId    = productId;

            return View(orderDetails);
        }


        public async Task<IActionResult> OrdersByStatus(string status)
        {
            ViewBag.Status = status;
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.AssignedEmployee)
                .Include(o => o.DeliveryMan)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .Where(o => o.OrderStatus == status)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> EmployeeProducts(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) return NotFound();

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductType)
                .Where(p => p.CreatedBy == employee.Email && (p.Status == "Approved" || p.Status == "Pending" || p.Status == "Active")) // Be more lenient for now
                .ToListAsync();

            ViewBag.EmployeeName = employee.Name;
            ViewBag.TotalProducts = products.Count;
            ViewBag.TotalValue = products.Sum(p => p.Price * p.StockQty);

            return View(products);
        }

        public async Task<IActionResult> EmployeeSalesDetails(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) return NotFound();

            var sales = await _context.Orders
                .Where(o => o.AssignedEmployeeId == employeeId && o.OrderStatus == "Delivered")
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.EmployeeName = employee.Name;
            return View(sales);
        }

        public async Task<IActionResult> EmployeeDashboard(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) return NotFound();

            var products = await _context.Products.Where(p => p.CreatedBy == employee.Name).ToListAsync();
            var sales = await _context.Orders
                .Where(o => o.AssignedEmployeeId == employeeId && o.OrderStatus == "Delivered")
                .Include(o => o.OrderDetails)
                .ToListAsync();

            var returns = await _context.Orders
                .Where(o => o.AssignedEmployeeId == employeeId && o.OrderStatus == "Returned") 
                .CountAsync();

            ViewBag.EmployeeName = employee.Name;
            ViewBag.TotalProducts = products.Count;
            ViewBag.TotalProductValue = products.Sum(p => p.Price * p.StockQty);
            ViewBag.TotalSalesCount = sales.Count;
            ViewBag.TotalRevenue = sales.Sum(s => s.TotalAmount);
            ViewBag.ReturnsCount = returns;

            return View(sales);
        }

        public IActionResult ManageBanners()
        {
            string bannerPath = Path.Combine(_env.WebRootPath, "images", "banners");
            if (!Directory.Exists(bannerPath))
            {
                Directory.CreateDirectory(bannerPath);
            }

            var banners = Directory.GetFiles(bannerPath)
                                   .Select(f => Path.GetFileName(f))
                                   .ToList();

            return View(banners);
        }

        [HttpPost]
        public async Task<IActionResult> UploadBanner(IFormFile bannerImage)
        {
            if (bannerImage != null && bannerImage.Length > 0)
            {
                string bannerPath = Path.Combine(_env.WebRootPath, "images", "banners");
                if (!Directory.Exists(bannerPath))
                {
                    Directory.CreateDirectory(bannerPath);
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(bannerImage.FileName);
                string fullPath = Path.Combine(bannerPath, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await bannerImage.CopyToAsync(stream);
                }
            }
            return RedirectToAction("ManageBanners");
        }

        [HttpPost]
        public IActionResult DeleteBanner(string fileName)
        {
            string fullPath = Path.Combine(_env.WebRootPath, "images", "banners", fileName);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
            return RedirectToAction("ManageBanners");
        }

        // SETTINGS: Footer & Payment
        public async Task<IActionResult> FooterSettings()
        {
            var footer = await _context.FooterInfos.FirstOrDefaultAsync();
            if (footer == null)
            {
                footer = new FooterInfo { Address = "Update Address", ContactNumber = "000", LastUpdated = DateTime.UtcNow };
                _context.FooterInfos.Add(footer);
                await _context.SaveChangesAsync();
            }
            return View(footer);
        }

        [HttpPost]
        public async Task<IActionResult> FooterSettings(FooterInfo model)
        {
            var footer = await _context.FooterInfos.FirstOrDefaultAsync();
            if (footer != null)
            {
                footer.Address = model.Address;
                footer.ContactNumber = model.ContactNumber;
                footer.Email = model.Email;
                footer.FacebookUrl = model.FacebookUrl;
                footer.InstagramUrl = model.InstagramUrl;
                footer.TwitterUrl = model.TwitterUrl;
                footer.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(FooterSettings));
        }

        public async Task<IActionResult> PaymentMethods()
        {
            var methods = await _context.PaymentMethods.ToListAsync();
            return View(methods);
        }

        [HttpPost]
        public async Task<IActionResult> AddPaymentMethod(PaymentMethod method, IFormFile? logoFile)
        {
            if (logoFile != null && logoFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "logo");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(logoFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }
                method.LogoUrl = "/images/logo/" + fileName;
            }

            method.CreatedAt = DateTime.UtcNow;
            method.Status = "Active";
            _context.PaymentMethods.Add(method);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PaymentMethods));
        }

        public async Task<IActionResult> EditPaymentMethod(int id)
        {
            var method = await _context.PaymentMethods.FindAsync(id);
            if (method == null) return NotFound();
            return View(method);
        }

        [HttpPost]
        public async Task<IActionResult> EditPaymentMethod(PaymentMethod method, IFormFile? logoFile)
        {
            var existing = await _context.PaymentMethods.FindAsync(method.Id);
            if (existing == null) return NotFound();

            existing.Name = method.Name;
            existing.Details = method.Details;
            existing.Status = method.Status;
            
            // Assume method.IsActive maps from Status if needed, but since it's just Name and Details
            // Update Active check
            existing.IsActive = existing.Status != "Inactive";

            if (logoFile != null && logoFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "logo");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(logoFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }
                existing.LogoUrl = "/images/logo/" + fileName;
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Payment Method updated successfully!";
            return RedirectToAction(nameof(PaymentMethods));
        }
        public async Task<IActionResult> ManageReviews()
        {
            var reviews = await _context.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var reviewViewModels = new List<ReviewViewModel>();
            foreach (var r in reviews)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == r.UserId);
                var product = await _context.Products.FindAsync(r.ProductId);

                reviewViewModels.Add(new ReviewViewModel
                {
                    Id = r.Id,
                    CustomerName = customer?.Name ?? "Anonymous",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    Status = r.Status,
                    ProductName = product?.Name ?? "Unknown Product",
                    CreatedAt = r.CreatedAt,
                    ImageUrls = r.ImageUrls
                });
            }
            return View(reviewViewModels);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                review.Status = "Active";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageReviews));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateReviewStatus(int id, string status)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                review.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageReviews));
        }

        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();
            return View(review);
        }

        [HttpPost, ActionName("DeleteReview")]
        public async Task<IActionResult> DeleteReviewConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageReviews));
        }

        public async Task<IActionResult> EmployeeList() => View("EmployeeSalesOverview", await GetEmployeePerformanceStats());

        public async Task<IActionResult> EmployeeSalesOverview()
        {
            return View("EmployeeSalesOverview", await GetEmployeePerformanceStats());
        }

        private async Task<List<EmployeePerformanceViewModel>> GetEmployeePerformanceStats()
        {
            var employees = await _context.Employees.ToListAsync();
            var stats = new List<EmployeePerformanceViewModel>();
            foreach (var emp in employees)
            {
                var productsManaged = await _context.Products.Where(p => p.CreatedBy == emp.Email || p.CreatedBy == emp.Name || p.AssignedEmployeeId == emp.Id).ToListAsync();
                var ordersAssigned = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.AssignedEmployeeId == emp.Id && o.OrderStatus == "Delivered")
                    .ToListAsync();

                stats.Add(new EmployeePerformanceViewModel
                {
                    EmployeeId = emp.Id,
                    EmployeeName = emp.Name,
                    TotalProductsManaged = productsManaged.Count,
                    TotalStockValue = productsManaged.Sum(p => p.Price * p.StockQty),
                    ProductsSold = ordersAssigned.SelectMany(o => o.OrderDetails ?? new List<OrderDetails>()).Sum(od => od.Quantity),
                    TotalSalesValue = ordersAssigned.Sum(o => o.TotalAmount),
                    ManagedProducts = productsManaged
                });
            }
            return stats;
        }

        public async Task<IActionResult> Messages()
        {
            var messages = await _context.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync();
            return View(messages);
        }

        public async Task<IActionResult> ReplyMessage(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();
            return View(msg);
        }

        [HttpPost]
        public async Task<IActionResult> ReplyMessage(int id, string reply)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg != null)
            {
                msg.Status = "Replied";
                // In a real app, you'd send an email here.
                await _context.SaveChangesAsync();
                TempData["Message"] = "Reply sent successfully.";
            }
            return RedirectToAction(nameof(Messages));
        }

        // Display Section Management
        public async Task<IActionResult> ManageDisplaySections()
        {
            var sections = await _context.DisplaySections
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();
            return View(sections);
        }

        // Create Display Section Pages
        public IActionResult CreateDisplaySection()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateDisplaySection(DisplaySection section)
        {
             if (ModelState.IsValid)
            {
                _context.DisplaySections.Add(section);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Display Section created successfully!";
                TempData["IsSuccess"] = true;
                return RedirectToAction(nameof(ManageDisplaySections));
            }
            return View(section);
        }

        public async Task<IActionResult> EditDisplaySection(int id)
        {
            var section = await _context.DisplaySections.FindAsync(id);
            if (section == null) return NotFound();
            return View(section);
        }

        [HttpPost]
        public async Task<IActionResult> EditDisplaySection(DisplaySection section)
        {
            var existing = await _context.DisplaySections.FindAsync(section.Id);
            if (existing != null)
            {
                existing.Name = section.Name;
                existing.DisplayOrder = section.DisplayOrder;
                existing.IsActive = section.IsActive;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Display Section updated successfully!";
                TempData["IsSuccess"] = true;
                return RedirectToAction(nameof(ManageDisplaySections));
            }
            return View(section);
        }

        public async Task<IActionResult> DeleteDisplaySection(int id)
        {
            var section = await _context.DisplaySections.FindAsync(id);
            if (section == null) return NotFound();
            return View(section);
        }

        [HttpPost, ActionName("DeleteDisplaySection")]
        public async Task<IActionResult> DeleteDisplaySectionConfirmed(int id)
        {
            var section = await _context.DisplaySections.FindAsync(id);
            if (section != null)
            {
                _context.DisplaySections.Remove(section);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Display Section deleted!";
                TempData["IsSuccess"] = true;
            }
            return RedirectToAction(nameof(ManageDisplaySections));
        }

        // Kept for backward compatibility if needed, but redirects to new flows
        [HttpPost]
        public async Task<IActionResult> AddDisplaySection(DisplaySection section)
        {
            return await CreateDisplaySection(section);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDisplaySection(DisplaySection section)
        {
             return await EditDisplaySection(section);
        }
        [HttpGet]
        public async Task<IActionResult> GetYearlySales(int year)
        {
            var monthlySales = new List<MonthlySalesViewModel>();
            var soldOrders = await _context.Orders
                .Where(o => o.CreatedAt.Year == year && (o.OrderStatus == "Delivered" || o.PaymentStatus == "Paid" || o.OrderStatus == "Shipped"))
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
    }
}
