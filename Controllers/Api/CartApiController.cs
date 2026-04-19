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
    public class CartApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public CartApiController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Ok(new { itemCount = 0, totalAmount = 0 });

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) return Ok(new { itemCount = 0, totalAmount = 0 });

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && c.Status == "Active");

            if (cart == null || cart.Items == null) return Ok(new { itemCount = 0, totalAmount = 0 });

            return Ok(new { 
                itemCount = cart.Items.Sum(i => i.Quantity),
                totalAmount = cart.Items.Sum(i => i.PriceWithVat * i.Quantity)
            });
        }
    }
}
