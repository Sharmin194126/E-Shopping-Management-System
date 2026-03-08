using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_ShoppingManagement.Controllers
{
    [Authorize]
    public class PaymentGatewayController : Controller
    {
        private readonly AppDbContext _context;

        public PaymentGatewayController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Initiate Payment Request to Gateway
        [HttpPost]
        public async Task<IActionResult> InitiatePayment(int orderId, string gateway)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            // Store intent in our DB
            var history = new PaymentHistory
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                GatewayName = gateway,
                Status = "Pending",
                TransactionId = $"TXN_{DateTime.Now.Ticks}", // placeholder
                PaymentDate = DateTime.Now
            };

            _context.PaymentHistories.Add(history);
            await _context.SaveChangesAsync();

            // MOCK: Generate Gateway redirection URL
            // In a real implementation (e.g., SSLCommerz), you would call their API here
            // and get a "GatewayPageURL".
            string gatewayUrl = $"https://gateway-mock.com/pay?amount={order.TotalAmount}&txn={history.TransactionId}&callback={Url.Action("PaymentCallback", "PaymentGateway", null, Request.Scheme)}";

            return Redirect(gatewayUrl);
        }

        // 2. Gateway Callback Handler (Success/Fail/Cancel)
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> PaymentCallback(string status, string txnId, string amount)
        {
            var history = await _context.PaymentHistories.FirstOrDefaultAsync(h => h.TransactionId == txnId);
            if (history == null) return BadRequest("Invalid Transaction");

            var order = await _context.Orders.FindAsync(history.OrderId);
            if (order == null) return NotFound();

            if (status == "Success")
            {
                history.Status = "Success";
                order.PaymentStatus = "Paid";
                order.OrderStatus = "Processing";
                
                // Funds are already in merchant account via Gateway
                // We just update the internal records
            }
            else
            {
                history.Status = status; // Failed / Cancelled
                order.PaymentStatus = "Failed";
            }

            _context.Update(history);
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Message"] = status == "Success" ? "Payment successful!" : "Payment failed. Please try again.";
            
            if (status == "Success")
            {
               return RedirectToAction("MoneyReceipt", "Order", new { id = order.Id });
            }

            return RedirectToAction("Index", "CustomerDashboard");
        }

        // 3. Admin View: Payment History
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminPaymentHistory()
        {
            var history = await _context.PaymentHistories
                .Include(h => h.Order)
                .OrderBy(h => h.PaymentDate)
                .ToListAsync();
            return View(history);
        }
    }
}
