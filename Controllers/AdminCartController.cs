using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using E_ShoppingManagement.Data;
using E_ShoppingManagement.ViewModels;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class AdminCartController : Controller
    {
        private readonly AppDbContext _context;

        public AdminCartController(AppDbContext context)
        {
            _context = context;
        }

        // All carts
        public async Task<IActionResult> Index()
        {
            var carts = await _context.Carts
                .Include(c => c.Customer)
                .Include(c => c.Items)
                .ToListAsync();

            var list = carts.Select(c => new CartViewModel
            {
                CartId = c.Id,
                CustomerId = c.CustomerId,
                CustomerName = c.Customer?.Name ?? "",
                Items = c.Items?.Select(ci => new CartItemViewModel
                {
                    CartItemId = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product?.Name ?? "",
                    UnitPrice = ci.Price,
                    Quantity = ci.Quantity,
                    ImageUrl = ci.Product?.ImageUrl
                }).ToList() ?? new List<CartItemViewModel>()
            }).ToList();

            return View(list);
        }

        // Details of a single cart
        public async Task<IActionResult> Details(int id)
        {
            var cart = await _context.Carts
                .Include(c => c.Customer)
                .Include(c => c.Items)!.ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cart == null) return NotFound();

            var vm = new CartViewModel
            {
                CartId = cart.Id,
                CustomerId = cart.CustomerId,
                CustomerName = cart.Customer?.Name ?? "",
                Items = cart.Items?.Select(ci => new CartItemViewModel
                {
                    CartItemId = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product?.Name ?? "",
                    UnitPrice = ci.Price,
                    Quantity = ci.Quantity,
                    ImageUrl = ci.Product?.ImageUrl
                }).ToList() ?? new List<CartItemViewModel>()
            };

            return View(vm);
        }

        // Admin remove an item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartItemId, int cartId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = cartId });
        }

        // Admin clear a cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart(int cartId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == cartId);

            if (cart?.Items != null)
            {
                _context.CartItems.RemoveRange(cart.Items);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = cartId });
        }

        // Admin Edit Cart
        public async Task<IActionResult> Edit(int id)
        {
            var cart = await _context.Carts
                .Include(c => c.Customer)
                .Include(c => c.Items)!.ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cart == null) return NotFound();

            var vm = new CartViewModel
            {
                CartId = cart.Id,
                CustomerName = cart.Customer?.Name ?? "",
                Items = cart.Items?.Select(ci => new CartItemViewModel
                {
                    CartItemId = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product?.Name ?? "",
                    UnitPrice = ci.Price,
                    Quantity = ci.Quantity,
                    ImageUrl = ci.Product?.ImageUrl
                }).ToList() ?? new List<CartItemViewModel>()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItemQuantity(int cartItemId, int quantity)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                item.Quantity = quantity;
                await _context.SaveChangesAsync();
            }
            return RedirectToActionResultOrIndex(item?.CartId);
        }

        private IActionResult RedirectToActionResultOrIndex(int? cartId)
        {
            if (cartId.HasValue) return RedirectToAction("Edit", new { id = cartId });
            return RedirectToAction("Index");
        }

        // Admin Delete Cart GET
        public async Task<IActionResult> Delete(int id)
        {
            var cart = await _context.Carts.Include(c => c.Customer).FirstOrDefaultAsync(c => c.Id == id);
            if (cart == null) return NotFound();
            return View(cart);
        }

        // Admin Delete Cart POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id);
            if (cart != null)
            {
                if (cart.Items != null) _context.CartItems.RemoveRange(cart.Items);
                _context.Carts.Remove(cart);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
