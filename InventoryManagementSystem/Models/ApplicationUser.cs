using Microsoft.AspNetCore.Identity;
using System;

namespace InventoryManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Cnic { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
