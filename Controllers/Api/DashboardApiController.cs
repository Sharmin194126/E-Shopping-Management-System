using E_ShoppingManagement.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_ShoppingManagement.Controllers.Api
{
    [Route("v1-api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Employee")]
    public class DashboardApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("goal-stats")]
        public async Task<IActionResult> GetYearlyGoalStats(int year)
        {
            var soldOrders = await _context.Orders
                .Where(o => o.CreatedAt.Year == year && (o.OrderStatus == "Delivered" || o.PaymentStatus == "Paid" || o.OrderStatus == "Shipped"))
                .ToListAsync();
            
            decimal totalRevenue = soldOrders.Sum(o => o.TotalAmount);
            decimal goalAmount = 500000; 

            return Ok(new { 
                year = year,
                revenue = totalRevenue,
                goal = goalAmount,
                percent = goalAmount > 0 ? (int)Math.Min(100, (totalRevenue / goalAmount) * 100) : 0,
                remaining = goalAmount - totalRevenue
            });
        }
    }
}
