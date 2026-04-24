using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using E_ShoppingManagement.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace E_ShoppingManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly AppDbContext _context;

        public AccountController(
            SignInManager<Users> signInManager,
            UserManager<Users> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
            _context = context;
        }

        // LOGIN GET
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // LOGIN POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
                return View(model);

            var user = await userManager.FindByEmailAsync(model.Email)
                       ?? await userManager.FindByNameAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
                return View(model);
            }

            /* 
               REMOVED: Do not automatically activate accounts on login. 
               Activation should only happen via Admin approval.
            */
            /*
            var customer = _context.Customers.FirstOrDefault(c => c.UserId == user.Id);
            if (customer != null) customer.Status = "Active";
            var employee = _context.Employees.FirstOrDefault(e => e.UserId == user.Id);
            if (employee != null) employee.Status = "Active";
            var dm = _context.DeliveryMen.FirstOrDefault(d => d.UserId == user.Id);
            if (dm != null) dm.Status = "Active";
            await _context.SaveChangesAsync();
            */

            // Check ReturnUrl first
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var roles = await userManager.GetRolesAsync(user);
            var roleList = roles.Select(r => (r ?? string.Empty).Trim()).ToList();

            // Status Check for Employee and DeliveryMan
            if (roleList.Contains("Employee"))
            {
                var emp = _context.Employees.FirstOrDefault(e => e.UserId == user.Id);
                if (emp != null && emp.Status != "Active")
                {
                    await signInManager.SignOutAsync();
                    ModelState.AddModelError(string.Empty, "Your employee account is pending admin approval.");
                    return View(model);
                }
            }
            if (roleList.Contains("DeliveryMan"))
            {
                var dm = _context.DeliveryMen.FirstOrDefault(d => d.UserId == user.Id);
                if (dm != null && dm.Status != "Active")
                {
                    await signInManager.SignOutAsync();
                    ModelState.AddModelError(string.Empty, "Your delivery man account is pending admin approval.");
                    return View(model);
                }
            }

            if (roleList.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                return RedirectToAction("Index", "AdminDashboard");

            if (roleList.Any(r => r.Equals("Employee", StringComparison.OrdinalIgnoreCase)))
                return RedirectToAction("Index", "EmployeeDashboard");

            if (roleList.Any(r => r.Equals("DeliveryMan", StringComparison.OrdinalIgnoreCase)))
                return RedirectToAction("Index", "DeliveryManDashboard");

            if (roleList.Any(r => r.Equals("Customer", StringComparison.OrdinalIgnoreCase)) ||
                roleList.Any(r => r.Equals("User", StringComparison.OrdinalIgnoreCase)))
                return RedirectToAction("Index", "CustomerDashboard");

            return RedirectToAction("Index", "Home");
        }


        // REGISTER GET (Selection Page)
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // USER REGISTER GET
        [HttpGet]
        public IActionResult UserRegister()
        {
            return View();
        }

        // REGISTER POST (Customer)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View("UserRegister", model); // Return to specific view if error

            var user = new Users
            {
                FullName = model.Name,
                UserName = model.Email,
                NormalizedUserName = model.Email.ToUpper(),
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpper(),
                PhoneNumber = model.PhoneNumber,
                Role = "Customer"
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Ensure Role
                if (!await roleManager.RoleExistsAsync("Customer"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Customer"));
                }
                await userManager.AddToRoleAsync(user, "Customer");

                // Create Customer Entity
                var customer = new Customer
                {
                    UserId = user.Id,
                    Name = model.Name,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Role = "Customer",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                await signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "CustomerDashboard");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View("UserRegister", model);
        }

        // EMPLOYEE REGISTER GET
        [HttpGet]
        public IActionResult EmployeeRegister()
        {
            return View();
        }

        // EMPLOYEE REGISTER POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeRegister(EmployeeRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new Users
            {
                FullName = model.Name,
                UserName = model.Email,
                NormalizedUserName = model.Email.ToUpper(),
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpper(),
                Role = "Employee"
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Ensure Role
                if (!await roleManager.RoleExistsAsync("Employee"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Employee"));
                }
                await userManager.AddToRoleAsync(user, "Employee");

                // Create Employee Entity
                var employee = new Employee
                {
                    UserId = user.Id,
                    Name = model.Name,
                    Email = model.Email,
                    Designation = model.Designation,
                    Salary = model.Salary,
                    PhoneNumber = model.PhoneNumber,
                    Status = "Pending",
                    JoiningDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Registration successful! Please wait for admin approval before logging in.";
                TempData["IsSuccess"] = true;
                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // DELIVERY MAN REGISTER GET
        [HttpGet]
        public IActionResult DeliveryManRegister()
        {
            return View();
        }

        // DELIVERY MAN REGISTER POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeliveryManRegister(DeliveryManRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new Users
            {
                FullName = model.Name,
                UserName = model.Email,
                NormalizedUserName = model.Email.ToUpper(),
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpper(),
                Role = "DeliveryMan"
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (!await roleManager.RoleExistsAsync("DeliveryMan"))
                {
                    await roleManager.CreateAsync(new IdentityRole("DeliveryMan"));
                }
                await userManager.AddToRoleAsync(user, "DeliveryMan");

                // Create DeliveryMan Entity
                var deliveryMan = new DeliveryMan
                {
                    UserId = user.Id,
                    Name = model.Name,
                    Email = model.Email,
                    ContactNumber = model.ContactNumber,
                    VehicleInfo = model.VehicleInfo,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                _context.DeliveryMen.Add(deliveryMan);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Registration successful! Please wait for admin approval before logging in.";
                TempData["IsSuccess"] = true;
                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }


        // VERIFY EMAIL + CHANGE PASSWORD same as before (ok)

        [HttpGet]
        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await userManager.FindByNameAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found!");
                return View(model);
            }

            return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
        }

        [HttpGet]
        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("VerifyEmail", "Account");

            return View(new ChangePasswordViewModel { Email = username });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Something went wrong");
                return View(model);
            }

            var user = await userManager.FindByNameAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found!");
                return View(model);
            }

            var result = await userManager.RemovePasswordAsync(user);
            if (result.Succeeded)
            {
                result = await userManager.AddPasswordAsync(user, model.NewPassword);
                if (result.Succeeded)
                    return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // LOGOUT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");

        }

        // LOGOUT GET (Confirmation Page)
        [HttpGet]
        [ActionName("Logout")]
        public IActionResult LogoutConfirmation()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View("Logout");
        }
    }
}
