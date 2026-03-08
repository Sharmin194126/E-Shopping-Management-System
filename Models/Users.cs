using Microsoft.AspNetCore.Identity;
namespace E_ShoppingManagement.Models
{
    public class Users : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Role { get; set; }
    }
}
