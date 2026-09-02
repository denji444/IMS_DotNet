using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Admin", "Customer", "Employee", "Supplier" };

            // Create roles if they don't exist
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create a default admin user if not present (helps with initial login)
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                var poweruser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@ims.com",
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                // Simple password for MVP testing
                var createPowerUser = await userManager.CreateAsync(poweruser, "Admin@123");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(poweruser, "Admin");
                }
            }
        }
    }
}
