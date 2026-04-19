using E_ShoppingManagement.Models;
using E_ShoppingManagement.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace E_ShoppingManagement.Controllers.Api
{
   
    [Route("v1-api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly SignInManager<Users> _signInManager;
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly E_ShoppingManagement.Data.AppDbContext _context;
        private readonly JwtTokenService _jwtTokenService;

        public AuthApiController(
            SignInManager<Users> signInManager,
            UserManager<Users> userManager,
            RoleManager<IdentityRole> roleManager,
            E_ShoppingManagement.Data.AppDbContext context,
            JwtTokenService jwtTokenService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _jwtTokenService = jwtTokenService;
        }

        // ─────────────────────────────────────────────────────────────
        // POST /v1-api/auth/login
        // ─────────────────────────────────────────────────────────────
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] ApiLoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return BadRequest(new ApiResponse(false, "Email and Password are required."));

            // Find by email OR username
            var user = await _userManager.FindByEmailAsync(model.Email)
                       ?? await _userManager.FindByNameAsync(model.Email);

            if (user == null)
                return Unauthorized(new ApiResponse(false, "Invalid Email or Password."));

            // Verify password
            var isValidPassword = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isValidPassword)
                return Unauthorized(new ApiResponse(false, "Invalid Email or Password."));

            // Check lockout
            if (await _userManager.IsLockedOutAsync(user))
                return StatusCode(423, new ApiResponse(false, "Account is locked out."));

            var roles = await _userManager.GetRolesAsync(user);

            // Generate JWT token
            var token = await _jwtTokenService.GenerateTokenAsync(user);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                token = token,
                expiresInDays = 7,
                user = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    phone = user.PhoneNumber,
                    roles = roles,
                    dashboardUrl = _jwtTokenService.GetDashboardUrl(roles)
                }
            });
        }

        // ─────────────────────────────────────────────────────────────
        // POST /v1-api/auth/register
        // Register new customer — returns JWT token
        // ─────────────────────────────────────────────────────────────
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] ApiRegisterModel model)
        {
            if (model == null)
                return BadRequest(new ApiResponse(false, "Invalid data."));

            if (string.IsNullOrWhiteSpace(model.Name) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
                return BadRequest(new ApiResponse(false, "Name, Email and Password are required."));

            if (model.Password != model.ConfirmPassword)
                return BadRequest(new ApiResponse(false, "Passwords do not match."));

            var user = new Users
            {
                FullName = model.Name,
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Role = "Customer"
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new ApiResponse(false, errors));
            }

            if (!await _roleManager.RoleExistsAsync("Customer"))
                await _roleManager.CreateAsync(new IdentityRole("Customer"));

            await _userManager.AddToRoleAsync(user, "Customer");

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

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtTokenService.GenerateTokenAsync(user);

            return Ok(new
            {
                success = true,
                message = "Registration successful",
                token = token,
                expiresInDays = 7,
                user = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    phone = user.PhoneNumber,
                    roles = roles,
                    dashboardUrl = "/CustomerDashboard"
                }
            });
        }

        // ─────────────────────────────────────────────────────────────
        // GET /v1-api/auth/profile
        // Returns logged-in user profile (requires JWT token)
        // ─────────────────────────────────────────────────────────────
        [HttpGet("profile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(new ApiResponse(false, "User not found."));

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    phone = user.PhoneNumber,
                    roles = roles
                }
            });
        }

        // ─────────────────────────────────────────────────────────────
        // POST /v1-api/auth/logout
        // For mobile: just discard the token on client side
        // ─────────────────────────────────────────────────────────────
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // JWT is stateless — client just deletes the token
            return Ok(new ApiResponse(true, "Logged out. Please delete your token on the client."));
        }

        // ── Request Models ──────────────────────────────────────────
        public class ApiLoginModel
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public bool RememberMe { get; set; }
        }

        public class ApiRegisterModel
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public record ApiResponse(bool Success, string Message);
    }
}
