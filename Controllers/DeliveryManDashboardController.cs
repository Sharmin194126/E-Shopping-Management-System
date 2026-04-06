using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "DeliveryMan,Admin")]
    public class DeliveryManDashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public DeliveryManDashboardController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ─────────────────────────────────────────
        //  HELPER: get current delivery man
        // ─────────────────────────────────────────
        private async Task<DeliveryMan?> GetCurrentDmAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.DeliveryMen.FirstOrDefaultAsync(d => d.UserId == user.Id);
        }

        // ─────────────────────────────────────────
        //  INDEX (Dashboard)
        // ─────────────────────────────────────────
        public async Task<IActionResult> Index(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            DeliveryMan? deliveryMan = null;

            if (id.HasValue && (User.IsInRole("Admin") || User.IsInRole("Employee")))
            {
                deliveryMan = await _context.DeliveryMen.FindAsync(id.Value);
            }
            else if (User.IsInRole("DeliveryMan"))
            {
                deliveryMan = await _context.DeliveryMen.FirstOrDefaultAsync(d => d.UserId == user.Id);
            }
            else if (User.IsInRole("Admin") || User.IsInRole("Employee"))
            {
                return RedirectToAction("Index", "DeliveryMan");
            }

            if (deliveryMan == null)
            {
                if (User.IsInRole("Admin") || User.IsInRole("Employee"))
                    return RedirectToAction("Index", "DeliveryMan");
                return NotFound("Delivery man profile not found.");
            }

            var assignedOrders = await _context.Orders
                .Where(o => o.DeliveryManId == deliveryMan.Id)
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.DeliveryMan = deliveryMan;
            ViewBag.DmName = deliveryMan.Name;
            ViewBag.UnreadNotifications = assignedOrders.Count(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-1));
            return View(assignedOrders);
        }

        // ─────────────────────────────────────────
        //  UPDATE STATUS
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string status, string? reason, string? paymentStatus)
        {
            var order = await _context.Orders.Include(o => o.DeliveryMan).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound();

            order.OrderStatus = status;
            if (paymentStatus != null) order.PaymentStatus = paymentStatus;

            if (status == "Delivered")
            {
                order.DeliveredAt = DateTime.UtcNow;
                if (order.PaymentStatus != "Paid") order.PaymentStatus = "Paid";

                if (order.DeliveryMan != null && order.DeliveryMan.CommissionRate > 0)
                {
                    var existingPayment = await _context.DeliveryManPayments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
                    if (existingPayment == null)
                    {
                        decimal commission = (order.TotalAmount * order.DeliveryMan.CommissionRate) / 100;
                        order.DeliveryMan.TotalEarnings += commission;
                        order.DeliveryMan.PendingAmount += commission;

                        var payment = new DeliveryManPayment
                        {
                            DeliveryManId = order.DeliveryMan.Id,
                            OrderId = order.Id,
                            OrderTotal = order.TotalAmount,
                            CommissionAmount = commission,
                            Status = "Pending"
                        };
                        _context.DeliveryManPayments.Add(payment);
                    }
                }
            }
            else if (status == "Returned")
            {
                order.ReturnReason = reason;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────
        //  EARNINGS
        // ─────────────────────────────────────────
        public async Task<IActionResult> Earnings()
        {
            var dm = await GetCurrentDmAsync();
            if (dm == null) return RedirectToAction("Login", "Account");

            var payments = await _context.DeliveryManPayments
                .Where(p => p.DeliveryManId == dm.Id)
                .Include(p => p.Order)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Build last-7-days chart data
            var labels = new List<string>();
            var data   = new List<decimal>();
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.UtcNow.AddDays(-i).Date;
                labels.Add(day.ToString("ddd dd"));
                data.Add(payments.Where(p => p.CreatedAt.Date == day).Sum(p => p.CommissionAmount));
            }

            ViewBag.DeliveryMan = dm;
            ViewBag.DmName = dm.Name;
            ViewBag.UnreadNotifications = 0;
            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(labels);
            ViewBag.ChartData   = System.Text.Json.JsonSerializer.Serialize(data);
            return View(payments);
        }

        // ─────────────────────────────────────────
        //  NOTIFICATIONS
        // ─────────────────────────────────────────
        public async Task<IActionResult> Notifications()
        {
            var dm = await GetCurrentDmAsync();
            if (dm == null) return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Where(o => o.DeliveryManId == dm.Id)
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.DmName = dm.Name;
            ViewBag.RecentOrders = orders;
            ViewBag.UnreadNotifications = orders.Count(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-1));
            return View();
        }

        [HttpPost]
        public IActionResult MarkAllRead()
        {
            // Placeholder — would clear a Notifications table in a real implementation
            return RedirectToAction(nameof(Notifications));
        }

        // ─────────────────────────────────────────
        //  PROFILE (View)
        // ─────────────────────────────────────────
        public async Task<IActionResult> Profile(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            DeliveryMan? dm = null;

            if (id.HasValue && User.IsInRole("Admin"))
                dm = await _context.DeliveryMen.FirstOrDefaultAsync(d => d.Id == id);
            else if (User.IsInRole("DeliveryMan"))
                dm = await _context.DeliveryMen.FirstOrDefaultAsync(d => d.UserId == user.Id);
            else if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "DeliveryMan");

            if (dm == null) return NotFound("Delivery Man Profile not found");

            ViewBag.DmName = dm.Name;
            ViewBag.UnreadNotifications = 0;
            return View(dm);
        }

        // ─────────────────────────────────────────
        //  PROFILE (Update)
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(int id, string name, string contactNumber, string? email, string? vehicleInfo)
        {
            var dm = await _context.DeliveryMen.FindAsync(id);
            if (dm == null) return NotFound();

            dm.Name = name;
            dm.ContactNumber = contactNumber;
            dm.Email = email;
            dm.VehicleInfo = vehicleInfo;

            // Also update the identity user email if provided
            if (!string.IsNullOrEmpty(email))
            {
                var user = await _userManager.FindByIdAsync(dm.UserId ?? "");
                if (user != null && user.Email != email)
                {
                    user.Email = email;
                    user.UserName = email;
                    await _userManager.UpdateAsync(user);
                }
            }

            await _context.SaveChangesAsync();
            TempData["ProfileSuccess"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // ─────────────────────────────────────────
        //  CHANGE PASSWORD
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["PwdError"] = "New password and confirmation do not match.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                TempData["PwdSuccess"] = "Password changed successfully.";
            }
            else
            {
                TempData["PwdError"] = string.Join(" ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Profile));
        }
    }
}
