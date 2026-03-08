using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using E_ShoppingManagement.ViewModels;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductController(AppDbContext context, IWebHostEnvironment env)
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

        private IEnumerable<SelectListItem> GetDisplayCategoryList()
        {
            return _context.DisplaySections
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new SelectListItem { Value = s.Name, Text = s.Name })
                .ToList();
        }

        private IEnumerable<SelectListItem> GetEmployeeList()
        {
            return _context.Employees
                .OrderBy(e => e.Name)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = e.Name
                }).ToList();
        }

        // INDEX with filter by Category & ProductType
        public IActionResult Index(int? categoryId, int? productTypeId, string? query)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.AssignedEmployee)
                .AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            if (productTypeId.HasValue && productTypeId.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.ProductTypeId == productTypeId.Value);
            }

            if (!string.IsNullOrEmpty(query))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(query));
            }

            ViewBag.CategoryList = new SelectList(_context.Categories.OrderBy(c => c.Name), "Id", "Name");
            ViewBag.ProductTypeList = new SelectList(_context.ProductTypes.OrderBy(pt => pt.Name), "Id", "Name");
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedProductTypeId = productTypeId;
            ViewBag.CurrentQuery = query;

            return View(productsQuery.ToList());
        }

        // DETAILS
        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // CREATE GET
        public IActionResult Create()
        {
            var vm = new ProductViewModel
            {
                CategoryList = GetCategoryList(),
                ProductTypeList = GetProductTypeList(),
                DisplayCategoryList = GetDisplayCategoryList(),
                EmployeeList = GetEmployeeList()
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
                model.EmployeeList = GetEmployeeList();
                return View(model);
            }

            string? fileName = null;
            if (model.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images/products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using var fileStream = new FileStream(filePath, FileMode.Create);
                model.ImageFile.CopyTo(fileStream);
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
                IsApproved = model.IsApproved,
                ImageUrl = fileName,
                DisplayCategory = model.DisplayCategory,
                AvailableSizes = model.AvailableSizes,
                VatPercentage = model.VatPercentage,
                OfferPercentage = model.OfferPercentage,
                MaxOrderQty = model.MaxOrderQty,
                AssignedEmployeeId = model.AssignedEmployeeId,
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

        // EDIT GET
        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
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
                IsApproved = product.IsApproved,
                CategoryList = GetCategoryList(),
                ProductTypeList = GetProductTypeList(),
                DisplayCategoryList = GetDisplayCategoryList(),
                EmployeeList = GetEmployeeList(),
                DisplayCategory = product.DisplayCategory,
                AvailableSizes = product.AvailableSizes,
                VatPercentage = product.VatPercentage,
                OfferPercentage = product.OfferPercentage,
                MaxOrderQty = product.MaxOrderQty,
                AssignedEmployeeId = product.AssignedEmployeeId,
                SizeStocks = _context.ProductSizeStocks.Where(s => s.ProductId == id).ToList()
            };

            // Fix for legacy data: if RegularPrice is 0 but Price is set, assume Price is the Regular Price
            if (vm.RegularPrice <= 0 && vm.Price > 0)
            {
                vm.RegularPrice = vm.Price;
                // If offer percentage is also 0, then Price matches RegularPrice (which is correct)
            }

            return View(vm);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Invalid data!";
                ViewBag.IsSuccess = false;
                model.CategoryList = GetCategoryList();
                model.ProductTypeList = GetProductTypeList();
                model.EmployeeList = GetEmployeeList();
                return View(model);
            }

            var product = _context.Products.Find(model.Id);
            if (product == null) return NotFound();

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
            product.IsApproved = model.IsApproved;
            product.DisplayCategory = model.DisplayCategory;
            product.AvailableSizes = model.AvailableSizes;
            product.VatPercentage = model.VatPercentage;
            product.OfferPercentage = model.OfferPercentage;
            product.MaxOrderQty = model.MaxOrderQty;
            product.AssignedEmployeeId = model.AssignedEmployeeId;
            product.ModifiedAt = DateTime.UtcNow;
            product.ModifiedBy = User.Identity?.Name;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images/products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

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

            ViewBag.Message = "Product updated successfully!";
            ViewBag.IsSuccess = true;

            model.CategoryList = GetCategoryList();
            model.ProductTypeList = GetProductTypeList();
            model.DisplayCategoryList = GetDisplayCategoryList();
            model.EmployeeList = GetEmployeeList();

            return View(model);
        }

        // DELETE GET
        public IActionResult Delete(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }

            TempData["Message"] = "Product deleted successfully.";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Index));
        }
    }
}
