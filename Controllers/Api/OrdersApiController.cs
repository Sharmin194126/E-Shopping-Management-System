using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_ShoppingManagement.Controllers.Api
{
    [Route("v1-api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class OrdersApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public OrdersApiController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) return Ok(new List<object>());

            var orders = await _context.Orders
                .Where(o => o.CustomerId == customer.Id)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new {
                    o.Id,
                    o.CreatedAt,
                    o.TotalAmount,
                    o.OrderStatus,
                    o.PaymentStatus,
                    ItemCount = o.OrderDetails.Sum(od => od.Quantity)
                })
                .ToListAsync();

            return Ok(orders);
        }
    }
}
