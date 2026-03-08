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
            else if (User.IsInRole("Admin") || User.IsInRole("Employee")) // Fallback if no ID provided for admin
            {
                 return RedirectToAction("Index", "DeliveryMan");
            }

            if (deliveryMan == null)
            {
                if (User.IsInRole("Admin") || User.IsInRole("Employee"))
                {
                    return RedirectToAction("Index", "DeliveryMan");
                }
                return NotFound("Delivery man profile not found.");
            }

            var assignedOrders = await _context.Orders
                .Where(o => o.DeliveryManId == deliveryMan.Id)
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.DeliveryMan = deliveryMan;
            return View(assignedOrders);
        }

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

        public async Task<IActionResult> Profile(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            DeliveryMan? dm = null;

            // Admin viewing specific profile
            if (id.HasValue && User.IsInRole("Admin"))
            {
                dm = await _context.DeliveryMen.FirstOrDefaultAsync(d => d.Id == id);
            }
            // DeliveryMan viewing their own profile
            else if (User.IsInRole("DeliveryMan"))
            {
                dm = await _context.DeliveryMen.FirstOrDefaultAsync(d => d.UserId == user.Id);
            }
            // Fallback for admin if no ID provided (redirect to list)
            else if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "DeliveryMan");
            }

            if (dm == null) return NotFound("Delivery Man Profile not found");

            return View(dm);
        }
    }
}
