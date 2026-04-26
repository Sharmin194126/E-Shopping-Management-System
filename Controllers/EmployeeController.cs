using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using E_ShoppingManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmployeeController(
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
            var employees = _context.Employees.ToList();
            return View(employees);
        }

        // DETAILS
        public IActionResult Details(int id)
        {
            var employee = _context.Employees.Find(id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        // CREATE GET
        public IActionResult Create()
        {
            var vm = new EmployeeViewModel
            {
                JoiningDate = DateTime.UtcNow,
                Status = "Active"
            };
            return View(vm);
        }

        // CREATE POST: Identity user + Employee + role(Employee)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required.");
                return View(model);
            }

            var user = new Users
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FullName = model.Name,
                Role = "Employee"
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                ViewBag.Message = "User create failed";
                ViewBag.IsSuccess = false;
                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync("Employee"))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole("Employee"));
                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    ViewBag.Message = "Role create failed";
                    ViewBag.IsSuccess = false;
                    return View(model);
                }
            }

            var addToRoleResult = await _userManager.AddToRoleAsync(user, "Employee");
            if (!addToRoleResult.Succeeded)
            {
                foreach (var error in addToRoleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                ViewBag.Message = "AddToRole failed";
                ViewBag.IsSuccess = false;
                return View(model);
            }

            var employee = new Employee
            {
                UserId = user.Id,
                Name = model.Name,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Designation = model.Designation,
                JoiningDate = model.JoiningDate,
                Salary = model.Salary,
                Status = model.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Employee created with login and role.";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        public IActionResult Edit(int id)
        {
            var employee = _context.Employees.Find(id);
            if (employee == null) return NotFound();

            var vm = new EmployeeViewModel
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                PhoneNumber = employee.PhoneNumber,
                Designation = employee.Designation,
                JoiningDate = employee.JoiningDate,
                Salary = employee.Salary,
                Status = employee.Status
            };

            return View(vm);
        }

        // EDIT POST: update Employee + Identity user; optional password reset
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeViewModel model)
        {
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

            var employee = _context.Employees.Find(model.Id);
            if (employee == null) return NotFound();

            employee.Name = model.Name;
            employee.Email = model.Email;
            employee.PhoneNumber = model.PhoneNumber;
            employee.Designation = model.Designation;
            employee.JoiningDate = model.JoiningDate;
            employee.Salary = model.Salary;
            employee.Status = model.Status;
            employee.ModifiedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(employee.UserId))
            {
                var user = await _userManager.FindByIdAsync(employee.UserId);
                if (user != null)
                {
                    user.FullName = model.Name;
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Role = "Employee";
                    await _userManager.UpdateAsync(user);

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

            ViewBag.Message = "Employee updated successfully!";
            ViewBag.IsSuccess = true;

            return View(model);
        }

        // DELETE GET
        public IActionResult Delete(int id)
        {
            var employee = _context.Employees.Find(id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // আবার fresh করে ডাটাবেজ থেকে নাও
            var employee = await _context.Employees
                                         .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                // row নেই, তাই কনকারেন্সি error না দিয়ে সimply Index এ ফিরে যাও
                TempData["Message"] = "Employee already deleted or not found.";
                TempData["IsSuccess"] = false;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Employee deleted successfully.";
                TempData["IsSuccess"] = true;
            }
            catch (DbUpdateConcurrencyException)
            {
                bool exists = await _context.Employees.AnyAsync(e => e.Id == id);
                if (!exists)
                {
                    TempData["Message"] = "Employee already deleted.";
                    TempData["IsSuccess"] = false;
                    return RedirectToAction(nameof(Index));
                }
                TempData["Message"] = "Concurrency error occurred while deleting employee.";
                TempData["IsSuccess"] = false;
            }

            return RedirectToAction(nameof(Index));
        }

        // APPROVE
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            employee.Status = "Active";
            employee.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Employee approved successfully. They can now log in.";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Index));
        }

        // DISAPPROVE
        [HttpPost]
        public async Task<IActionResult> Disapprove(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            employee.Status = "Pending";
            employee.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Employee disapproved. They can no longer log in.";
            TempData["IsSuccess"] = true;

            return RedirectToAction(nameof(Index));
        }
    }
}
