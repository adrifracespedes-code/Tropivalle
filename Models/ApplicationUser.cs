using Microsoft.AspNetCore.Identity;

namespace EcommerceApp.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
