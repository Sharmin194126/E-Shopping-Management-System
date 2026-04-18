using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class DeliveryManController : Controller
    {
        private readonly AppDbContext _context;

        public DeliveryManController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.DeliveryMen.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeliveryMan deliveryMan)
        {
            if (ModelState.IsValid)
            {
                _context.Add(deliveryMan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(deliveryMan);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var deliveryMan = await _context.DeliveryMen.FindAsync(id);
            if (deliveryMan == null) return NotFound();
            return View(deliveryMan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DeliveryMan deliveryMan)
        {
            if (id != deliveryMan.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(deliveryMan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(deliveryMan);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var deliveryMan = await _context.DeliveryMen.FindAsync(id);
            if (deliveryMan != null)
            {
                _context.DeliveryMen.Remove(deliveryMan);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var deliverer = await _context.DeliveryMen.FindAsync(id);
            if (deliverer == null) return NotFound();

            var orders = await _context.Orders
                .Where(o => o.DeliveryManId == id)
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.DeliveryMan = deliverer;
            ViewBag.Delivered = orders.Count(o => o.OrderStatus == "Delivered");
            ViewBag.Pending = orders.Count(o => o.OrderStatus == "Shipping" || o.OrderStatus == "Processed" || o.OrderStatus == "Shipped");
            ViewBag.Returned = orders.Count(o => o.OrderStatus == "Returned");

            return View(orders);
        }

        public async Task<IActionResult> Payments(int id)
        {
            var dm = await _context.DeliveryMen.FindAsync(id);
            if (dm == null) return NotFound();

            var payments = await _context.DeliveryManPayments
                .Where(p => p.DeliveryManId == id)
                .Include(p => p.Order)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.DeliveryMan = dm;
            return View(payments);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayout(int id, decimal amount)
        {
            var dm = await _context.DeliveryMen.FindAsync(id);
            if (dm == null || amount <= 0) return BadRequest();

            if (amount > dm.PendingAmount) amount = dm.PendingAmount;

            dm.PaidAmount += amount;
            dm.PendingAmount -= amount;

            var pendingPayments = await _context.DeliveryManPayments
                .Where(p => p.DeliveryManId == id && p.Status == "Pending")
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            decimal remaining = amount;
            foreach (var p in pendingPayments)
            {
                if (remaining >= p.CommissionAmount)
                {
                    p.Status = "Paid";
                    p.PaidAt = DateTime.Now;
                    remaining -= p.CommissionAmount;
                }
                else break;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Payments), new { id });
        }
    }
}
