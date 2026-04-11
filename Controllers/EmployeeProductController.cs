using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using E_ShoppingManagement.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EmployeeProductController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private IEnumerable<SelectListItem> GetCategoryList()
        {
            return _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
        }

        private IEnumerable<SelectListItem> GetProductTypeList()
        {
            return _context.ProductTypes
                .OrderBy(pt => pt.Name)
                .Select(pt => new SelectListItem
                {
                    Value = pt.Id.ToString(),
                    Text = pt.Name
                }).ToList();
        }

        private IEnumerable<SelectListItem> GetStatusList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "Approved", Text = "Approved" },
                new SelectListItem { Value = "Rejected", Text = "Rejected" }
            };
        }

        private IEnumerable<SelectListItem> GetDisplayCategoryList()
        {
            return _context.DisplaySections
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new SelectListItem { Value = s.Name, Text = s.Name })
                .ToList();
        }

        // GET: EmployeeProduct
        public async Task<IActionResult> Index()
        {
            var userEmail = User.Identity?.Name;
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductType)
                .Where(p => p.CreatedBy == userEmail) // Only show employee's own products
                .ToListAsync();
            return View(products);
        }

        // CREATE GET
        public IActionResult Create()
        {
            var vm = new ProductViewModel
            {
                CategoryList = GetCategoryList(),
                ProductTypeList = GetProductTypeList(),
                DisplayCategoryList = GetDisplayCategoryList(),
                StatusList = GetStatusList()
            };
            return View(vm);
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CategoryList = GetCategoryList();
                model.ProductTypeList = GetProductTypeList();
                model.DisplayCategoryList = GetDisplayCategoryList();
                model.StatusList = GetStatusList();
                return View(model);
            }

            string? fileName = "/images/products/no-image.png";
            if (model.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images/products");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.ImageFile.CopyTo(stream);
                }
                fileName = "/images/products/" + fileName;
            }

            decimal calculatedPrice = model.RegularPrice;
            if (model.OfferPercentage > 0)
            {
                calculatedPrice = model.RegularPrice - (model.RegularPrice * (model.OfferPercentage / 100));
            }

            var product = new Product
            {
                Name = model.Name,
                CategoryId = model.CategoryId,
                ProductTypeId = model.ProductTypeId,
                Price = calculatedPrice,
                RegularPrice = model.RegularPrice,
                StockQty = model.StockQty,
                Description = model.Description,
                Status = model.Status,
                DisplayCategory = model.DisplayCategory,
                AvailableSizes = model.AvailableSizes,
                IsApproved = model.Status == "Approved", 
                ImageUrl = fileName,
                CreatedBy = User.Identity?.Name,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            _context.SaveChanges();

            // Save Size-wise stock
            if (model.SizeStocks != null && model.SizeStocks.Any())
            {
                foreach (var ss in model.SizeStocks)
                {
                    ss.ProductId = product.Id;
                    _context.ProductSizeStocks.Add(ss);
                }
                _context.SaveChanges();
            }

            TempData["Message"] = "Product created successfully.";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Index));
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductType)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            ViewBag.SizeStocks = await _context.ProductSizeStocks.Where(ss => ss.ProductId == id).ToListAsync();
            return View(product);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var vm = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                ProductTypeId = product.ProductTypeId,
                Price = product.Price,
                RegularPrice = product.RegularPrice,
                StockQty = product.StockQty,
                Description = product.Description,
                Status = product.Status,
                DisplayCategory = product.DisplayCategory,
                AvailableSizes = product.AvailableSizes,
                IsApproved = product.IsApproved,
                CategoryList = GetCategoryList(),
                ProductTypeList = GetProductTypeList(),
                DisplayCategoryList = GetDisplayCategoryList(),
                StatusList = GetStatusList(),
                SizeStocks = await _context.ProductSizeStocks.Where(s => s.ProductId == id).ToListAsync(),
                ExistingImageUrl = product.ImageUrl
            };

            return View(vm);
        }

        // GET: EditStock
        public async Task<IActionResult> EditStock(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: UpdateStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int id, int stockQty)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.StockQty = stockQty;
            product.ModifiedAt = DateTime.UtcNow;
            product.ModifiedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();
            TempData["Message"] = "Stock updated successfully!";
            TempData["IsSuccess"] = true;
            return RedirectToAction(nameof(Index));
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductViewModel model)
        {
            var product = _context.Products.Find(model.Id);
            if (product == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.CategoryList = GetCategoryList();
                model.ProductTypeList = GetProductTypeList();
                model.DisplayCategoryList = GetDisplayCategoryList();
                model.StatusList = GetStatusList();
                return View(model);
            }

            decimal calculatedPrice = model.RegularPrice;
            if (model.OfferPercentage > 0)
            {
                calculatedPrice = model.RegularPrice - (model.RegularPrice * (model.OfferPercentage / 100));
            }

            product.Name = model.Name;
            product.CategoryId = model.CategoryId;
            product.ProductTypeId = model.ProductTypeId;
            product.Price = calculatedPrice;
            product.RegularPrice = model.RegularPrice;
            product.StockQty = model.StockQty;
            product.Description = model.Description;
            product.Status = model.Status;
            product.DisplayCategory = model.DisplayCategory;
            product.AvailableSizes = model.AvailableSizes;
            product.IsApproved = model.Status == "Approved";
            product.ModifiedAt = DateTime.UtcNow;
            product.ModifiedBy = User.Identity?.Name;

            if (model.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.ImageFile.CopyTo(stream);
                }
                product.ImageUrl = "/images/products/" + fileName;
            }

            _context.SaveChanges();

            // Update Size-wise stock
            var oldStocks = _context.ProductSizeStocks.Where(s => s.ProductId == product.Id).ToList();
            _context.ProductSizeStocks.RemoveRange(oldStocks);
            
            if (model.SizeStocks != null && model.SizeStocks.Any())
            {
                foreach (var ss in model.SizeStocks)
                {
                    ss.ProductId = product.Id;
                    ss.Id = 0; // Ensure it's treated as a new record
                    _context.ProductSizeStocks.Add(ss);
                }
            }
            _context.SaveChanges();

            TempData["Message"] = "Product updated successfully!";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Index));
        }

        // GET: EmployeeProduct/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductType)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            // Only allow deleting own products
            if (product.CreatedBy != User.Identity?.Name) return Unauthorized();

            ViewBag.SizeStocks = await _context.ProductSizeStocks.Where(ss => ss.ProductId == id).ToListAsync();
            return View(product);
        }

        // POST: EmployeeProduct/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Security check
            if (product.CreatedBy != User.Identity?.Name) return Unauthorized();

            // Clear size stocks first
            var sizeStocks = _context.ProductSizeStocks.Where(ss => ss.ProductId == id);
            _context.ProductSizeStocks.RemoveRange(sizeStocks);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Product deleted successfully.";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Index));
        }
    }
}

