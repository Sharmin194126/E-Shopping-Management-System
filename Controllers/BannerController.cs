using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BannerController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public BannerController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            string bannerPath = Path.Combine(_env.WebRootPath, "images", "banners");
            if (!Directory.Exists(bannerPath))
            {
                Directory.CreateDirectory(bannerPath);
            }

            var images = Directory.GetFiles(bannerPath)
                                  .Select(f => Path.GetFileName(f))
                                  .ToList();

            return View(images);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(List<IFormFile> bannerImages)
        {
            if (bannerImages != null && bannerImages.Count > 0)
            {
                string bannerPath = Path.Combine(_env.WebRootPath, "images", "banners");
                if (!Directory.Exists(bannerPath))
                {
                    Directory.CreateDirectory(bannerPath);
                }

                foreach (var file in bannerImages)
                {
                    if (file.Length > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string fullPath = Path.Combine(bannerPath, fileName);

                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                    }
                }
                TempData["Message"] = "Banners uploaded successfully!";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(string fileName)
        {
            string bannerPath = Path.Combine(_env.WebRootPath, "images", "banners");
            string fullPath = Path.Combine(bannerPath, fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                TempData["Message"] = "Banner deleted successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}
