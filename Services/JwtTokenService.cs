using E_ShoppingManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace E_ShoppingManagement.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _config;
        private readonly UserManager<Users> _userManager;

        public JwtTokenService(IConfiguration config, UserManager<Users> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        public async Task<string> GenerateTokenAsync(Users user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
                new Claim("fullName", user.FullName ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            // Add roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiration = DateTime.UtcNow.AddDays(
                int.Parse(_config["JwtSettings:ExpirationDays"] ?? "7"));

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GetDashboardUrl(IList<string> roles)
        {
            if (roles.Contains("Admin")) return "/AdminDashboard";
            if (roles.Contains("Employee")) return "/EmployeeDashboard";
            if (roles.Contains("DeliveryMan")) return "/DeliveryManDashboard";
            return "/CustomerDashboard";
        }
    }
}
