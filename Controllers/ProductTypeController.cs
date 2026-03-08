using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using E_ShoppingManagement.ViewModels;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class ProductTypeController : Controller
    {
        private readonly AppDbContext _context;

        public ProductTypeController(AppDbContext context)
        {
            _context = context;
        }

        // INDEX
        public IActionResult Index()
        {
            var productTypes = _context.ProductTypes.ToList();
            return View(productTypes);
        }

        // DETAILS
        public IActionResult Details(int id)
        {
            var productType = _context.ProductTypes.Find(id);
            if (productType == null) return NotFound();
            return View(productType);
        }

        // CREATE GET
        public IActionResult Create()
        {
            var vm = new ProductTypeViewModel();
            return View(vm);
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductTypeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var productType = new ProductType
            {
                Name = model.Name,
                Description = model.Description,
                Status = model.Status,
                IsApproved = model.IsApproved,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductTypes.Add(productType);
            _context.SaveChanges();

            TempData["Message"] = "Product type created successfully.";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        public IActionResult Edit(int id)
        {
            var productType = _context.ProductTypes.Find(id);
            if (productType == null) return NotFound();

            var vm = new ProductTypeViewModel
            {
                Id = productType.Id,
                Name = productType.Name,
                Description = productType.Description,
                Status = productType.Status,
                IsApproved = productType.IsApproved
            };

            return View(vm);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Invalid data!";
                ViewBag.IsSuccess = false;
                return View(model);
            }

            var productType = _context.ProductTypes.Find(model.Id);
            if (productType == null) return NotFound();

            productType.Name = model.Name;
            productType.Description = model.Description;
            productType.Status = model.Status;
            productType.IsApproved = model.IsApproved;
            productType.ModifiedAt = DateTime.UtcNow;

            _context.SaveChanges();

            ViewBag.Message = "Product type updated successfully!";
            ViewBag.IsSuccess = true;

            return View(model);
        }

        // DELETE GET
        public IActionResult Delete(int id)
        {
            var productType = _context.ProductTypes.Find(id);
            if (productType == null) return NotFound();
            return View(productType);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var productType = _context.ProductTypes.Find(id);
            if (productType != null)
            {
                _context.ProductTypes.Remove(productType);
                _context.SaveChanges();
            }

            TempData["Message"] = "Product type deleted successfully.";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Index));
        }
    }
}
