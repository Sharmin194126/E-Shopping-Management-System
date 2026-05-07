using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using E_ShoppingManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace E_ShoppingManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<Users> _userManager;

        public HomeController(ILogger<HomeController> logger, AppDbContext context, IWebHostEnvironment env, UserManager<Users> userManager)
        {
            _logger = logger;
            _context = context;
            _env = env;
            _userManager = userManager;
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) return NotFound();

            // Fetch order status updates for this customer
            var orders = await _context.Orders
                .Where(o => o.CustomerId == customer.Id)
                .OrderByDescending(o => o.ModifiedAt ?? o.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductType)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var reviews = await _context.Reviews
                .Where(r => r.ProductId == id && r.Status == "Active")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var relatedProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductType)
                .Where(p => p.Id != id && p.CategoryId == product.CategoryId && p.Status == "Active")
                .Take(5)
                .ToListAsync();

            var ratingSummary = new Dictionary<int, int>();
            for (int i = 1; i <= 5; i++)
            {
                ratingSummary[i] = reviews.Count(r => r.Rating == i);
            }

            var reviewViewModels = new List<ReviewViewModel>();
            foreach (var r in reviews)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == r.UserId);
                var reviewVm = new ReviewViewModel
                {
                    Id = r.Id,
                    CustomerName = customer?.Name ?? "Anonymous",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    ImageUrls = r.ImageUrls,
                    LikeCount = await _context.ReviewReactions.CountAsync(rr => rr.ReviewId == r.Id && rr.ReactionType == "Like"),
                    DislikeCount = await _context.ReviewReactions.CountAsync(rr => rr.ReviewId == r.Id && rr.ReactionType == "Dislike"),
                    IsLikedByCurrentUser = User.Identity?.IsAuthenticated == true && await _context.ReviewReactions.AnyAsync(rr => rr.ReviewId == r.Id && rr.UserId == _userManager.GetUserId(User) && rr.ReactionType == "Like"),
                    IsDislikedByCurrentUser = User.Identity?.IsAuthenticated == true && await _context.ReviewReactions.AnyAsync(rr => rr.ReviewId == r.Id && rr.UserId == _userManager.GetUserId(User) && rr.ReactionType == "Dislike"),
                    Replies = await _context.ReviewReplies.Where(rp => rp.ReviewId == r.Id).OrderBy(rp => rp.CreatedAt).Select(rp => new ReviewReplyViewModel {
                        Id = rp.Id,
                        ReplyText = rp.ReplyText,
                        CreatedAt = rp.CreatedAt,
                        IsSeller = rp.IsSeller
                    }).ToListAsync()
                };

                // Add replier names
                foreach(var reply in reviewVm.Replies) {
                    var replierReview = await _context.ReviewReplies.FindAsync(reply.Id);
                    if (replierReview != null) {
                        var user = await _userManager.FindByIdAsync(replierReview.UserId);
                        reply.ReplierName = user?.UserName ?? "Staff";
                    }
                }
                reviewViewModels.Add(reviewVm);
            }

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                Reviews = reviewViewModels,
                RelatedProducts = relatedProducts,
                RatingSummary = ratingSummary,
                TotalReviews = reviews.Count,
                SizeStocks = await _context.ProductSizeStocks.Where(ss => ss.ProductId == id).ToListAsync(),
                RelatedImages = await _context.ProductImages.Where(pi => pi.ProductId == id).Select(pi => pi.ImageUrl).ToListAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeIndexViewModel();

            // Get Banner Images
            string bannerPath = Path.Combine(_env.WebRootPath, "images", "banners");
            if (Directory.Exists(bannerPath))
            {
                viewModel.Banners = Directory.GetFiles(bannerPath)
                                        .Select(f => "/images/banners/" + Path.GetFileName(f))
                                        .ToList();
            }

            viewModel.Products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductType)
                .Where(p => p.Status == "Approved" || p.Status == "Pending" || p.Status == "Active")
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            viewModel.DisplaySections = await _context.DisplaySections
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            // Auto-seed category section so it appears in admin
            if (!await _context.DisplaySections.AnyAsync(s => s.Name.Contains("Categories")))
            {
                var catSection = new DisplaySection { Name = "Shop by Categories", DisplayOrder = 0, IsActive = true };
                _context.DisplaySections.Add(catSection);
                await _context.SaveChangesAsync();
                if (catSection.IsActive) {
                    viewModel.DisplaySections.Insert(0, catSection);
                }
            }

            // Seed default sections if none exist
            if (viewModel.DisplaySections.Count(s => !s.Name.Contains("Categories")) == 0)
            {
                var defaults = new List<DisplaySection>
                {
                    new DisplaySection { Name = "Featured Items", DisplayOrder = 1 },
                    new DisplaySection { Name = "Exclusive Items", DisplayOrder = 2 },
                    new DisplaySection { Name = "Offer Price", DisplayOrder = 3 },
                    new DisplaySection { Name = "Just For You", DisplayOrder = 4 },
                    new DisplaySection { Name = "Restock", DisplayOrder = 5 }
                };
                _context.DisplaySections.AddRange(defaults);
                await _context.SaveChangesAsync();
                viewModel.DisplaySections.AddRange(defaults);
                viewModel.DisplaySections = viewModel.DisplaySections.OrderBy(s => s.DisplayOrder).ToList();
            }

            viewModel.FooterInfo = await _context.FooterInfos.FirstOrDefaultAsync();
            viewModel.PaymentMethods = await _context.PaymentMethods.Where(pm => pm.IsActive).ToListAsync();
            viewModel.Categories = await _context.Categories.Where(c => c.Status == "Active").ToListAsync();

            // Fetch recent reviews with customer details
            var recentReviews = await _context.Reviews
                .Where(r => r.Status == "Active")
                .OrderByDescending(r => r.CreatedAt)
                .Take(6)
                .ToListAsync();

            var reviewViewModels = new List<ReviewViewModel>();
            foreach (var r in recentReviews)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == r.UserId);
                
                var reviewVm = new ReviewViewModel
                {
                    CustomerName = customer?.Name ?? "Anonymous",
                    ProfilePicture = customer?.ProfilePictureUrl,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    ImageUrls = r.ImageUrls,
                    LikeCount = await _context.ReviewReactions.CountAsync(rr => rr.ReviewId == r.Id && rr.ReactionType == "Like"),
                    DislikeCount = await _context.ReviewReactions.CountAsync(rr => rr.ReviewId == r.Id && rr.ReactionType == "Dislike"),
                    IsLikedByCurrentUser = User.Identity?.IsAuthenticated == true && await _context.ReviewReactions.AnyAsync(rr => rr.ReviewId == r.Id && rr.UserId == _userManager.GetUserId(User) && rr.ReactionType == "Like"),
                    IsDislikedByCurrentUser = User.Identity?.IsAuthenticated == true && await _context.ReviewReactions.AnyAsync(rr => rr.ReviewId == r.Id && rr.UserId == _userManager.GetUserId(User) && rr.ReactionType == "Dislike"),
                    Replies = await _context.ReviewReplies.Where(rp => rp.ReviewId == r.Id).OrderBy(rp => rp.CreatedAt).Select(rp => new ReviewReplyViewModel {
                        Id = rp.Id,
                        ReplyText = rp.ReplyText,
                        CreatedAt = rp.CreatedAt,
                        IsSeller = rp.IsSeller
                        // ReplierName will be handled below if needed, but for now we'll assume system names
                    }).ToListAsync()
                };

                // Add replier names
                foreach(var reply in reviewVm.Replies) {
                    var replierReview = await _context.ReviewReplies.FindAsync(reply.Id);
                    if (replierReview != null) {
                        var user = await _userManager.FindByIdAsync(replierReview.UserId);
                        reply.ReplierName = user?.UserName ?? "Staff";
                    }
                }

                reviewViewModels.Add(reviewVm);
            }
            viewModel.Reviews = reviewViewModels;

            return View(viewModel);
        }

        public async Task<IActionResult> Search(string query)
        {
            return await CategoryProducts(null, null, null, null, query);
        }

        public async Task<IActionResult> CategoryProducts(int? categoryId, int? typeId, string? size, string? priceRange, string? query)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductType)
                .Where(p => p.Status == "Active")
                .AsQueryable();

            if (categoryId.HasValue) productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);
            if (typeId.HasValue) productsQuery = productsQuery.Where(p => p.ProductTypeId == typeId);
            if (!string.IsNullOrEmpty(size)) productsQuery = productsQuery.Where(p => p.AvailableSizes != null && p.AvailableSizes.Contains(size));
            if (!string.IsNullOrEmpty(query))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(query) || p.Description.Contains(query));
            }

            if (!string.IsNullOrEmpty(priceRange))
            {
                switch (priceRange)
                {
                    case "low": productsQuery = productsQuery.Where(p => p.Price < 500); break;
                    case "mid": productsQuery = productsQuery.Where(p => p.Price >= 500 && p.Price <= 2000); break;
                    case "high": productsQuery = productsQuery.Where(p => p.Price > 2000); break;
                    case "very-high": productsQuery = productsQuery.Where(p => p.Price > 10000); break;
                }
            }

            var user = await _userManager.GetUserAsync(User);
            int employeeId = 0;
            if (user != null && User.IsInRole("Employee"))
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
                employeeId = employee?.Id ?? 0;
            }

            var vm = new CategoryProductsViewModel
            {
                Products = await productsQuery.OrderByDescending(p => p.CreatedAt).ToListAsync(),
                Categories = await _context.Categories.ToListAsync(),
                ProductTypes = await _context.ProductTypes.ToListAsync(),
                SelectedCategory = categoryId,
                SelectedType = typeId,
                SelectedSize = size,
                PriceRange = priceRange,
                SearchQuery = query,
                LoggedInEmployeeId = employeeId
            };

            return View("CategoryProducts", vm);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string comment, List<IFormFile>? images)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!User.IsInRole("Customer"))
            {
                TempData["Error"] = "Only customers can submit reviews.";
                return RedirectToAction("Details", new { id = productId });
            }

            // Handle image uploads
            var imageUrls = new List<string>();
            if (images != null && images.Any())
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "reviews");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                foreach (var image in images.Take(5)) // Limit to 5 images
                {
                    if (image.Length > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                        string filePath = Path.Combine(uploadsFolder, fileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }
                        imageUrls.Add("/uploads/reviews/" + fileName);
                    }
                }
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = user.Id,
                Rating = rating,
                Comment = comment,
                ImageUrls = imageUrls.Any() ? string.Join(",", imageUrls) : null,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Refresh product average rating
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                var allReviews = await _context.Reviews.Where(r => r.ProductId == productId).ToListAsync();
                product.AverageRating = allReviews.Average(r => r.Rating);
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "Thank you for your review!";
            return RedirectToAction("Details", new { id = productId });
        }


        public IActionResult Privacy() => View();
        public IActionResult HelpCenter() => View();
        public IActionResult Returns() => View();
        [HttpGet]
        public IActionResult Tracking() => View();

        [HttpPost]
        public async Task<IActionResult> Tracking(int orderId, string phoneNumber)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.PhoneNumber == phoneNumber);

            if (order == null)
            {
                ViewBag.Error = "Order not found or phone number does not match.";
                return View();
            }

            return View(order);
        }
        public IActionResult Contact() => View();
        public IActionResult Email() => View();

        public async Task<IActionResult> Deals()
        {
            var dealProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductType)
                .Where(p => (p.Status == "Active" || p.Status == "Approved") && p.OfferPercentage > 0)
                .OrderByDescending(p => p.OfferPercentage)
                .ToListAsync();

            // If no explicit offer products, show top-discounted by price diff
            if (!dealProducts.Any())
            {
                dealProducts = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductType)
                    .Where(p => p.Status == "Active" || p.Status == "Approved")
                    .OrderByDescending(p => p.RegularPrice - p.Price)
                    .Take(12)
                    .ToListAsync();
            }

            return View(dealProducts);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string name, string email, string subject, string message, IFormFile? attachment, string messageType = "General")
        {
            string? attachmentUrl = null;
            if (attachment != null && attachment.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "attachments");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string fileName = Guid.NewGuid().ToString() + "_" + attachment.FileName;
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await attachment.CopyToAsync(fileStream);
                }
                attachmentUrl = "/uploads/attachments/" + fileName;
            }

            var contactMsg = new ContactMessage
            {
                Name = name,
                Email = email,
                Subject = subject,
                Message = message,
                MessageType = messageType,
                AttachmentUrl = attachmentUrl,
                CreatedAt = DateTime.UtcNow,
                Status = "Active"
            };

            _context.ContactMessages.Add(contactMsg);
            await _context.SaveChangesAsync();

            TempData["Message"] = messageType == "Email" ? "Your email has been sent successfully!" : "Your message has been sent successfully! We will contact you soon.";
            return RedirectToAction(messageType == "Email" ? "Email" : "Contact");
        }
        [HttpPost]
        public async Task<IActionResult> SendLiveChatMessage(string message)
        {
            var user = await _userManager.GetUserAsync(User);
            var contactMsg = new ContactMessage
            {
                Name = user?.FullName ?? "Customer",
                Email = user?.Email ?? "N/A",
                Subject = "Live Chat Inquiry",
                Message = message,
                MessageType = "LiveChat",
                CreatedAt = DateTime.UtcNow,
                Status = "Active"
            };
 
            _context.ContactMessages.Add(contactMsg);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}

