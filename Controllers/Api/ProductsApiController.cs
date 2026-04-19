using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_ShoppingManagement.Controllers.Api
{
    [Route("v1-api/[controller]")]
    [ApiController]
    public class ProductsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(int? categoryId, string? query)
        {
            var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();
            if (categoryId.HasValue) productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            if (!string.IsNullOrEmpty(query)) productsQuery = productsQuery.Where(p => p.Name.Contains(query));
            return await productsQuery.ToListAsync();
        }
    }
}
