using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using E_ShoppingManagement.ViewModels;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CustomerController(
            AppDbContext context,
            UserManager<Users> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // INDEX
        public IActionResult Index()
        {
            var customers = _context.Customers.Where(c => c.Role == "Customer").ToList();
            return View(customers);
        }

        // DETAILS
        public IActionResult Details(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        // CREATE GET
        public IActionResult Create()
        {
            var vm = new CustomerViewModel
            {
                Status = "Active"
            };
            return View(vm);
        }

        // CREATE POST: create Identity user + Customer + assign Customer role
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 1) Create Identity user
            var user = new Users
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FullName = model.Name,
                Role = "User"
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            // 2) Ensure Customer role exists
            if (!await _roleManager.RoleExistsAsync("Customer"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Customer"));
            }

            // 3) Assign Customer role
            await _userManager.AddToRoleAsync(user, "Customer"); // [web:118][web:129]

            // 4) Create Customer row linked to AspNetUsers
            var customer = new Customer
            {
                UserId = user.Id,
                Name = model.Name,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Role = "Customer",
                Status = model.Status,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Customer created with login and role.";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        public IActionResult Edit(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null) return NotFound();

            var vm = new CustomerViewModel
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                Status = customer.Status
                // Password fields used only if admin wants to reset in Edit
            };

            return View(vm);
        }

        // EDIT POST: update Customer + Identity; optional password reset
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerViewModel model)
        {
            // allow empty password on edit
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Invalid data!";
                ViewBag.IsSuccess = false;
                return View(model);
            }

            var customer = _context.Customers.Find(model.Id);
            if (customer == null) return NotFound();

            // Update Customer table
            customer.Name = model.Name;
            customer.Email = model.Email;
            customer.PhoneNumber = model.PhoneNumber;
            customer.Status = model.Status;
            customer.ModifiedAt = DateTime.UtcNow;
            customer.ModifiedBy = User.Identity?.Name;

            // Update Identity user
            if (!string.IsNullOrEmpty(customer.UserId))
            {
                var user = await _userManager.FindByIdAsync(customer.UserId);
                if (user != null)
                {
                    user.FullName = model.Name;
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Role = "User";
                    await _userManager.UpdateAsync(user);

                    // If new password entered, reset it
                    if (!string.IsNullOrWhiteSpace(model.Password))
                    {
                        var hasPassword = await _userManager.HasPasswordAsync(user);
                        if (hasPassword)
                        {
                            var removeResult = await _userManager.RemovePasswordAsync(user);
                            if (!removeResult.Succeeded)
                            {
                                foreach (var err in removeResult.Errors)
                                    ModelState.AddModelError("", err.Description);
                                ViewBag.Message = "Error changing password.";
                                ViewBag.IsSuccess = false;
                                return View(model);
                            }
                        }

                        var addResult = await _userManager.AddPasswordAsync(user, model.Password);
                        if (!addResult.Succeeded)
                        {
                            foreach (var err in addResult.Errors)
                                ModelState.AddModelError("", err.Description);
                            ViewBag.Message = "Error setting new password.";
                            ViewBag.IsSuccess = false;
                            return View(model);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            ViewBag.Message = "Customer updated successfully!";
            ViewBag.IsSuccess = true;

            return View(model);
        }

        // DELETE GET
        public IActionResult Delete(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        // DELETE POST: remove Customer and Identity user
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer != null)
            {
                if (!string.IsNullOrEmpty(customer.UserId))
                {
                    var user = await _userManager.FindByIdAsync(customer.UserId);
                    if (user != null)
                    {
                        await _userManager.DeleteAsync(user);
                    }
                }

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "Customer deleted successfully.";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceLogout(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null && !string.IsNullOrEmpty(customer.UserId))
            {
                var user = await _userManager.FindByIdAsync(customer.UserId);
                if (user != null)
                {
                    await _userManager.UpdateSecurityStampAsync(user);
                }
                customer.Status = "Inactive";
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "Customer session invalidated (Logged out).";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Index));
        }
    }
}
