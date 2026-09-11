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

            // Seed 12+ realistic test products & registered purchases for live scenario testing
            await SeedSampleProductsAndPurchasesAsync(serviceProvider);
        }

        public static async Task SeedSampleProductsAndPurchasesAsync(IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<InventoryDbContext>();

            // 1. Ensure Suppliers exist
            var supplierApple = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Name == "Apple Direct Importers");
            if (supplierApple == null)
            {
                supplierApple = new Supplier
                {
                    Name = "Apple Direct Importers",
                    ContactName = "Farhan Malik",
                    Email = "imports@appledirect.com",
                    Phone = "03001234567",
                    Address = "Shop 14, Hall Road, Lahore",
                    IsEmailVerified = true
                };
                dbContext.Suppliers.Add(supplierApple);
            }

            var supplierGlobal = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Name == "Global Tech Traders");
            if (supplierGlobal == null)
            {
                supplierGlobal = new Supplier
                {
                    Name = "Global Tech Traders",
                    ContactName = "Usman Ghani",
                    Email = "info@globaltech.com",
                    Phone = "03219876543",
                    Address = "Suite 402, Hafeez Centre, Gulberg III, Lahore",
                    IsEmailVerified = true
                };
                dbContext.Suppliers.Add(supplierGlobal);
            }

            await dbContext.SaveChangesAsync();

            // Get existing or fallback suppliers
            var fallbackSupplier = await dbContext.Suppliers.FirstOrDefaultAsync() ?? supplierApple;

            // 2. Define 12 Realistic Test Products
            var sampleProducts = new[]
            {
                new {
                    Name = "iPhone 17 Pro Max",
                    Variant = "256GB - Orange (89% Health, 9/10 Condition)",
                    Sku = "IP17PM-256-ORG-89",
                    Desc = "Super Retina XDR display, Titanium frame with Orange finish, 89% battery health, 9/10 body condition.",
                    Category = "Mobile Phones",
                    Type = "PTA Approved",
                    Price = 385000.00m,
                    Cost = 350000.00m,
                    Qty = 3,
                    SupplierId = supplierApple.Id,
                    Batch = "BATCH-IP17-01",
                    PayMethod = PaymentMethod.Cash
                },
                new {
                    Name = "iPhone 16 Pro",
                    Variant = "128GB - Natural Titanium (95% Health, 10/10)",
                    Sku = "IP16P-128-NT-95",
                    Desc = "A17 Pro Chip, Natural Titanium finish, 95% battery health, flawless 10/10 mint condition.",
                    Category = "Mobile Phones",
                    Type = "Non-PTA (Factory Unlocked)",
                    Price = 295000.00m,
                    Cost = 265000.00m,
                    Qty = 5,
                    SupplierId = fallbackSupplier.Id,
                    Batch = "BATCH-IP16-02",
                    PayMethod = PaymentMethod.OnlineTransfer
                },
                new {
                    Name = "Samsung Galaxy S24 Ultra",
                    Variant = "512GB - Titanium Black (100% Health, Box Packed)",
                    Sku = "S24U-512-BLK-CPID",
                    Desc = "Galaxy AI, Snapdragon 8 Gen 3, S-Pen included, CPID Approved, Box Packed.",
                    Category = "Mobile Phones",
                    Type = "CPID Approved",
                    Price = 310000.00m,
                    Cost = 280000.00m,
                    Qty = 4,
                    SupplierId = supplierGlobal.Id,
                    Batch = "BATCH-S24U-01",
                    PayMethod = PaymentMethod.Cash
                },
                new {
                    Name = "Google Pixel 9 Pro XL",
                    Variant = "256GB - Obsidian Black (92% Health, 9.5/10)",
                    Sku = "PIX9PXL-256-BLK",
                    Desc = "Tensor G4 Chip, Pro Triple Camera System, Obsidian Black, 92% Health.",
                    Category = "Mobile Phones",
                    Type = "Patch Approved",
                    Price = 240000.00m,
                    Cost = 215000.00m,
                    Qty = 2,
                    SupplierId = supplierGlobal.Id,
                    Batch = "BATCH-PIX9-01",
                    PayMethod = PaymentMethod.Cash
                },
                new {
                    Name = "MacBook Pro 16\" M3 Max",
                    Variant = "36GB RAM / 1TB SSD - Space Black",
                    Sku = "MBP16-M3MAX-36-1TB",
                    Desc = "M3 Max 16-Core CPU, 40-Core GPU, 36GB Unified Memory, Liquid Retina XDR display.",
                    Category = "Laptops & Computers",
                    Type = "Brand New (Box Packed)",
                    Price = 850000.00m,
                    Cost = 790000.00m,
                    Qty = 2,
                    SupplierId = supplierApple.Id,
                    Batch = "BATCH-MBP16-01",
                    PayMethod = PaymentMethod.OnlineTransfer
                },
                new {
                    Name = "Dell XPS 15 9530",
                    Variant = "Core i9 13th Gen / 32GB RAM / 1TB SSD / RTX 4070",
                    Sku = "DELL-XPS15-I9-4070",
                    Desc = "OLED 3.5K Touch Screen, Intel Core i9-13900H, NVIDIA RTX 4070 8GB.",
                    Category = "Laptops & Computers",
                    Type = "Open Box (Imported)",
                    Price = 420000.00m,
                    Cost = 380000.00m,
                    Qty = 3,
                    SupplierId = fallbackSupplier.Id,
                    Batch = "BATCH-XPS15-01",
                    PayMethod = PaymentMethod.Cash
                },
                new {
                    Name = "Lenovo ThinkPad X1 Carbon Gen 11",
                    Variant = "Core i7 13th Gen / 16GB RAM / 512GB SSD",
                    Sku = "THINK-X1C11-I7-16-512",
                    Desc = "Ultrabook, 14\" WUXGA Anti-Glare display, Carbon Fiber chassis, Grade A Refurbished.",
                    Category = "Laptops & Computers",
                    Type = "Refurbished (Grade A)",
                    Price = 275000.00m,
                    Cost = 240000.00m,
                    Qty = 6,
                    SupplierId = fallbackSupplier.Id,
                    Batch = "BATCH-X1C-01",
                    PayMethod = PaymentMethod.Cash
                },
                new {
                    Name = "Sony WH-1000XM5 Wireless Headphones",
                    Variant = "Midnight Blue - Sealed Box",
                    Sku = "SONY-WH1000XM5-BLU",
                    Desc = "Industry leading noise canceling headphones with Auto NC Optimizer, 30-hr battery life.",
                    Category = "Electronics & Accessories",
                    Type = "Original / Genuine",
                    Price = 85000.00m,
                    Cost = 72000.00m,
                    Qty = 10,
                    SupplierId = supplierGlobal.Id,
                    Batch = "BATCH-SONY-01",
                    PayMethod = PaymentMethod.Cash
                },
                new {
                    Name = "Apple Watch Ultra 2",
                    Variant = "49mm Titanium Case with Orange Ocean Band",
                    Sku = "AW-ULTRA2-49-ORG",
                    Desc = "S9 SiP, double tap gesture, brightest Apple display ever (3000 nits), Water resistant 100m.",
                    Category = "Electronics & Accessories",
                    Type = "Original / Genuine",
                    Price = 215000.00m,
                    Cost = 190000.00m,
                    Qty = 4,
                    SupplierId = supplierApple.Id,
                    Batch = "BATCH-AWU2-01",
                    PayMethod = PaymentMethod.Cash
                },
                new {
                    Name = "iPad Pro 13\" M4",
                    Variant = "256GB WiFi + Cellular - Space Black",
                    Sku = "IPAD-M4-13-256-BLK",
                    Desc = "Ultra Retina XDR Tandem OLED, M4 chip, Ultra thin 5.1mm design, Apple Pencil Pro support.",
                    Category = "Laptops & Computers",
                    Type = "Brand New (Box Packed)",
                    Price = 360000.00m,
                    Cost = 325000.00m,
                    Qty = 3,
                    SupplierId = supplierApple.Id,
                    Batch = "BATCH-IPAD-01",
                    PayMethod = PaymentMethod.OnlineTransfer
                },
                new {
                    Name = "Anker 737 Power Bank (PowerCore 24K)",
                    Variant = "24,000mAh 140W Fast Charging",
                    Sku = "ANKER-737-24K-140W",
                    Desc = "Smart digital display, 140W 2-way fast charging for laptops & phones.",
                    Category = "Electronics & Accessories",
                    Type = "Original / Genuine",
                    Price = 35000.00m,
                    Cost = 28000.00m,
                    Qty = 15,
                    SupplierId = supplierGlobal.Id,
                    Batch = "BATCH-ANKER-01",
                    PayMethod = PaymentMethod.Cash
                },
                new {
                    Name = "PlayStation 5 Slim Digital Edition",
                    Variant = "1TB SSD - White (Japanese Spec)",
                    Sku = "PS5-SLIM-DIG-1TB",
                    Desc = "Slim design, 1TB custom SSD, DualSense wireless controller included.",
                    Category = "Electronics & Accessories",
                    Type = "Brand New (Box Packed)",
                    Price = 155000.00m,
                    Cost = 135000.00m,
                    Qty = 5,
                    SupplierId = fallbackSupplier.Id,
                    Batch = "BATCH-PS5-01",
                    PayMethod = PaymentMethod.Cash
                }
            };

            int purchaseIndex = 101;
            foreach (var item in sampleProducts)
            {
                var existingProd = await dbContext.Products.FirstOrDefaultAsync(p => p.Sku == item.Sku);
                if (existingProd == null)
                {
                    existingProd = new Product
                    {
                        Name = item.Name,
                        Variant = item.Variant,
                        Sku = item.Sku,
                        Description = item.Desc,
                        CategoryName = item.Category,
                        ProductType = item.Type,
                        Price = item.Price,
                        StockQuantity = item.Qty
                    };
                    dbContext.Products.Add(existingProd);
                    await dbContext.SaveChangesAsync();

                    var purchase = new Purchase
                    {
                        PurchaseNo = $"PUR-TEST-{purchaseIndex++}",
                        ProductId = existingProd.Id,
                        SupplierId = item.SupplierId,
                        Quantity = item.Qty,
                        UnitPrice = item.Cost,
                        TotalCost = item.Qty * item.Cost,
                        PurchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 15)),
                        Notes = $"Initial stock purchase for {item.Name} ({item.Variant})",
                        BatchNumber = item.Batch,
                        PaymentMode = PaymentMode.FullPayment,
                        PaymentMethod = item.PayMethod
                    };
                    dbContext.Purchases.Add(purchase);
                }
            }

            await dbContext.SaveChangesAsync();
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
