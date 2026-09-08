using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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

            // Ensure all existing user accounts have EmailConfirmed set to true
            var unconfirmedUsers = userManager.Users.Where(u => !u.EmailConfirmed).ToList();
            foreach (var user in unconfirmedUsers)
            {
                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user);
            }

            // Seed default Product Categories & Types
            await SeedProductCategoriesAsync(serviceProvider);
        }

        private static async Task SeedProductCategoriesAsync(IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<InventoryDbContext>();
            if (!await dbContext.ProductCategories.AnyAsync())
            {
                var mobileCategory = new ProductCategory
                {
                    Name = "Mobile Phones",
                    Description = "Smartphones, feature phones, and mobile devices.",
                    TypeOptions = new System.Collections.Generic.List<ProductCategoryTypeOption>
                    {
                        new ProductCategoryTypeOption { TypeName = "PTA Approved" },
                        new ProductCategoryTypeOption { TypeName = "CPID Approved" },
                        new ProductCategoryTypeOption { TypeName = "Patch Approved" },
                        new ProductCategoryTypeOption { TypeName = "JV (SIM Locked)" },
                        new ProductCategoryTypeOption { TypeName = "Non-PTA (Factory Unlocked)" }
                    }
                };

                var laptopCategory = new ProductCategory
                {
                    Name = "Laptops & Computers",
                    Description = "Laptops, desktops, workstations, and computing hardware.",
                    TypeOptions = new System.Collections.Generic.List<ProductCategoryTypeOption>
                    {
                        new ProductCategoryTypeOption { TypeName = "Brand New (Box Packed)" },
                        new ProductCategoryTypeOption { TypeName = "Open Box (Imported)" },
                        new ProductCategoryTypeOption { TypeName = "Refurbished (Grade A)" },
                        new ProductCategoryTypeOption { TypeName = "Used (9/10 Condition)" }
                    }
                };

                var bookCategory = new ProductCategory
                {
                    Name = "Books & Publications",
                    Description = "Books, manuals, novels, and printed publications.",
                    TypeOptions = new System.Collections.Generic.List<ProductCategoryTypeOption>
                    {
                        new ProductCategoryTypeOption { TypeName = "Hardcover (Original)" },
                        new ProductCategoryTypeOption { TypeName = "Paperback / Softcover" },
                        new ProductCategoryTypeOption { TypeName = "Reprint Edition" }
                    }
                };

                var electronicsCategory = new ProductCategory
                {
                    Name = "Electronics & Accessories",
                    Description = "Chargers, cables, audio gear, and electronic gadgets.",
                    TypeOptions = new System.Collections.Generic.List<ProductCategoryTypeOption>
                    {
                        new ProductCategoryTypeOption { TypeName = "Original / Genuine" },
                        new ProductCategoryTypeOption { TypeName = "Copy / Master Copy" },
                        new ProductCategoryTypeOption { TypeName = "Refurbished" }
                    }
                };

                var generalCategory = new ProductCategory
                {
                    Name = "General Stock",
                    Description = "General merchandise and miscellaneous inventory.",
                    TypeOptions = new System.Collections.Generic.List<ProductCategoryTypeOption>
                    {
                        new ProductCategoryTypeOption { TypeName = "Standard" }
                    }
                };

                dbContext.ProductCategories.AddRange(mobileCategory, laptopCategory, bookCategory, electronicsCategory, generalCategory);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
