using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CategoryController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // INDEX
        public IActionResult Index()
        {
            return View(_context.Categories.ToList());
        }

        // CREATE GET
        public IActionResult Create()
        {
            return View();
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.ImageFile != null)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
                string path = Path.Combine(_env.WebRootPath, "images/categories");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                using var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create);
                model.ImageFile.CopyTo(stream);

                model.ImageUrl = fileName;
            }

            _context.Categories.Add(model);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        public IActionResult Edit(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category model)
        {
            ModelState.Remove("ImageUrl");

            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Invalid data!";
                ViewBag.IsSuccess = false;
                return View(model);
            }

            var category = _context.Categories.Find(model.Id);
            if (category == null) return NotFound();

            category.Name = model.Name;
            category.Description = model.Description;
            category.Status = model.Status;
            category.ModifiedAt = DateTime.UtcNow;

            if (model.ImageFile != null)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
                string path = Path.Combine(_env.WebRootPath, "images/categories");

                using var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create);
                model.ImageFile.CopyTo(stream);

                category.ImageUrl = fileName;
            }

            _context.SaveChanges();

            ViewBag.Message = "Category updated successfully!";
            ViewBag.IsSuccess = true;

            return View(category);
        }

        // DETAILS
        public IActionResult Details(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // DELETE GET
        public IActionResult Delete(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
