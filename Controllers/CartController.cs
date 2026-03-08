using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using E_ShoppingManagement.ViewModels;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Admin,Employee,Customer,DeliveryMan")]
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public CartController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            
            // If user is Admin or Employee and doesn't have a Customer entry, create one.
            if (customer == null && (User.IsInRole("Admin") || User.IsInRole("Employee")))
            {
                customer = new Customer
                {
                    UserId = user.Id,
                    Name = user.FullName ?? user.UserName ?? "User",
                    Email = user.Email ?? "",
                    Role = User.IsInRole("Admin") ? "Admin" : "Employee",
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active"
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            return customer;
        }

        private async Task<Cart> GetOrCreateCartAsync(int customerId)
        {
            var cart = await _context.Carts
                                     .Include(c => c.Items!)
                                     .ThenInclude(ci => ci.Product)
                                     .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Status == "Active");

            if (cart == null)
            {
                cart = new Cart
                {
                    CustomerId = customerId,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active"
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Login", "Account");

            var cart = await _context.Carts
                                     .Include(c => c.Items!)
                                     .ThenInclude(ci => ci.Product)
                                     .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && c.Status == "Active");

            var vm = new CartViewModel
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                CartId = cart?.Id ?? 0
            };

            if (cart != null && cart.Items != null)
            {
                vm.Items = cart.Items.Select(ci => new CartItemViewModel
                {
                    CartItemId = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product?.Name ?? "",
                    ImageUrl = ci.Product?.ImageUrl,
                    UnitPrice = ci.Price,
                    VatAmount = ci.VatAmount,
                    PriceWithVat = ci.PriceWithVat,
                    Quantity = ci.Quantity,
                    Size = ci.Size
                }).ToList();
            }
            return View(vm);
        }

        // POST: Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1, string? size = null)
        {
            var result = await AddToCartLogic(productId, quantity, size);
            if (result != null) return result;

            return RedirectToAction("Index");
        }

        // GET: Cart/BuyNow/{id}
        // Handles "Buy Now" and "Add to Cart" links where login redirection might convert POST to GET
        [HttpGet]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1, string? size = null)
        {
             var result = await AddToCartLogic(productId, quantity, size);
             if (result != null) return result;
             
             // For "Buy Now", immediately checkout
             return RedirectToAction("Checkout", "Order");
        }

        // GET: Cart/AddToCart/{id}
        // Handles "Add to Cart" links (GET request)
        [HttpGet]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, string? size = null)
        {
             var result = await AddToCartLogic(productId, quantity, size);
             if (result != null) return result;
             
             return RedirectToAction("Index");
        }

        private async Task<IActionResult?> AddToCartLogic(int productId, int quantity, string? size)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Login", "Account");

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var cart = await GetOrCreateCartAsync(customer.Id);

            // Check if item with same Product AND Size exists
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId && ci.Size == size);

            if (existingItem == null)
            {
                var vatAmount = product.Price * (product.VatPercentage / 100);
                var item = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = product.Id,
                    Quantity = quantity,
                    Price = product.Price,
                    VatAmount = vatAmount,
                    PriceWithVat = product.Price + vatAmount,
                    Size = size
                };
                _context.CartItems.Add(item);
            }
            else
            {
                existingItem.Quantity += quantity;
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Product added to cart.";
            TempData["IsSuccess"] = true;

            return null; // pure success
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            var item = await _context.CartItems
                .Include(ci => ci.Cart)
                .ThenInclude(c => c!.Customer)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

            if (item == null) return NotFound();

            if (quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSize(int cartItemId, string size)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) return NotFound();

            item.Size = size;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // POST: Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Login", "Account");

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && c.Status == "Active");

            if (cart?.Items != null)
            {
                _context.CartItems.RemoveRange(cart.Items);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
