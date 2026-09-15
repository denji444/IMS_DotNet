using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Exceptions;

namespace InventoryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly InventoryDbContext _context;

        public ProductsController(InventoryDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // DataTables Endpoint
        [HttpGet]
        public async Task<IActionResult> GetProductsData()
        {
            var products = await _context.Products
                .Select(p => new
                {
                    p.Id,
                    p.Sku,
                    p.Name,
                    p.Variant,
                    CategoryName = string.IsNullOrEmpty(p.CategoryName) ? "General Stock" : p.CategoryName,
                    ProductType = string.IsNullOrEmpty(p.ProductType) ? "Standard" : p.ProductType,
                    p.Description,
                    p.Price,
                    p.StockQuantity
                })
                .ToListAsync();
            return Json(new { data = products });
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoriesWithTypes()
        {
            var categories = await _context.ProductCategories
                .Include(c => c.TypeOptions)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    typeOptions = c.TypeOptions.Select(t => t.TypeName).ToList()
                })
                .ToListAsync();

            return Json(categories);
        }

        [HttpGet]
        public async Task<IActionResult> GetTypesByCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return Json(new System.Collections.Generic.List<string>());
            }

            var types = await _context.ProductCategoryTypeOptions
                .Where(t => t.Category != null && t.Category.Name == categoryName)
                .Select(t => t.TypeName)
                .ToListAsync();

            return Json(types);
        }

        // Select2 Endpoint for autocomplete
        [HttpGet]
        public async Task<IActionResult> GetProductsJson(string? q)
        {
            var query = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(p => p.Sku.Contains(q) || p.Name.Contains(q) || p.Variant.Contains(q));
            }
            var products = await query.ToListAsync();

            var productIds = products.Select(p => p.Id).ToList();
            var latestPurchasePrices = await _context.Purchases
                .Where(p => p.ProductId.HasValue && productIds.Contains(p.ProductId.Value))
                .GroupBy(p => p.ProductId!.Value)
                .Select(g => new
                {
                    ProductId = g.Key,
                    UnitPrice = g.OrderByDescending(p => p.PurchaseDate).Select(p => p.UnitPrice).FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.ProductId, x => x.UnitPrice);

            var data = products.Select(p =>
            {
                decimal? price = p.Price;
                if ((!price.HasValue || price.Value == 0) && latestPurchasePrices.TryGetValue(p.Id, out var purcPrice))
                {
                    price = purcPrice;
                }

                return new
                {
                    id = p.Id,
                    text = string.IsNullOrEmpty(p.Variant) ? $"{p.Name} ({p.Sku})" : $"{p.Name} ({p.Variant}) [{p.Sku}]",
                    price = price
                };
            }).ToList();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductsFiltered(string? categoryName, string? productType, string? q)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                query = query.Where(p => p.CategoryName == categoryName);
            }

            if (!string.IsNullOrWhiteSpace(productType))
            {
                query = query.Where(p => p.ProductType == productType);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => p.Sku.Contains(q) || p.Name.Contains(q) || p.Variant.Contains(q));
            }

            var products = await query.ToListAsync();
            var productIds = products.Select(p => p.Id).ToList();

            var latestPurchasePrices = await _context.Purchases
                .Where(p => p.ProductId.HasValue && productIds.Contains(p.ProductId.Value))
                .GroupBy(p => p.ProductId!.Value)
                .Select(g => new
                {
                    ProductId = g.Key,
                    UnitPrice = g.OrderByDescending(p => p.PurchaseDate).Select(p => p.UnitPrice).FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.ProductId, x => x.UnitPrice);

            var data = products.Select(p =>
            {
                decimal? price = p.Price;
                if ((!price.HasValue || price.Value == 0) && latestPurchasePrices.TryGetValue(p.Id, out var purcPrice))
                {
                    price = purcPrice;
                }

                return new
                {
                    id = p.Id,
                    sku = p.Sku,
                    name = p.Name,
                    variant = p.Variant,
                    categoryName = p.CategoryName ?? "",
                    productType = p.ProductType ?? "",
                    text = string.IsNullOrEmpty(p.Variant) ? $"{p.Name} ({p.Sku})" : $"{p.Name} ({p.Variant}) [{p.Sku}]",
                    price = price ?? 0,
                    stockQuantity = p.StockQuantity
                };
            }).ToList();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (product == null)
            {
                return NotFound();
            }

            decimal? effectivePrice = product.Price;
            if (!effectivePrice.HasValue || effectivePrice.Value == 0)
            {
                // Fallback to latest purchase unit price
                effectivePrice = await _context.Purchases
                    .Where(p => p.ProductId == id)
                    .OrderByDescending(p => p.PurchaseDate)
                    .Select(p => (decimal?)p.UnitPrice)
                    .FirstOrDefaultAsync();
            }

            return Json(new
            {
                product.Id,
                product.Sku,
                product.Name,
                product.Variant,
                CategoryName = product.CategoryName ?? "",
                ProductType = product.ProductType ?? "",
                product.Description,
                Price = effectivePrice,
                product.StockQuantity
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            // Verify main SKU manually
            if (await _context.Products.AnyAsync(p => p.Sku == product.Sku))
            {
                return Json(new { success = false, message = $"SKU '{product.Sku}' already exists." });
            }

            if (product.MultipleVariants != null && product.MultipleVariants.Any())
            {
                // Manual validation check for multiple variants list
                foreach (var variant in product.MultipleVariants)
                {
                    if (string.IsNullOrWhiteSpace(variant.Sku))
                    {
                        return Json(new { success = false, message = "All variants must have a SKU." });
                    }
                    if (string.IsNullOrWhiteSpace(variant.Variant))
                    {
                        return Json(new { success = false, message = "All variants must have a Variant name." });
                    }
                    if (variant.Sku == product.Sku)
                    {
                        return Json(new { success = false, message = $"Variant SKU '{variant.Sku}' cannot be the same as the main SKU." });
                    }
                    if (await _context.Products.AnyAsync(p => p.Sku == variant.Sku))
                    {
                        return Json(new { success = false, message = $"SKU '{variant.Sku}' already exists." });
                    }
                }

                // Check list itself for duplicate SKUs
                var duplicateSkus = product.MultipleVariants.GroupBy(v => v.Sku).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                if (duplicateSkus.Any())
                {
                    return Json(new { success = false, message = $"Duplicate SKUs inside submission: {string.Join(", ", duplicateSkus)}" });
                }
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Save main product
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    // Save additional variants
                    if (product.MultipleVariants != null && product.MultipleVariants.Any())
                    {
                        foreach (var variant in product.MultipleVariants)
                        {
                            var newProduct = new Product
                            {
                                Sku = variant.Sku.Trim(),
                                Name = product.Name.Trim(),
                                Variant = variant.Variant.Trim(),
                                CategoryName = product.CategoryName,
                                ProductType = product.ProductType,
                                Description = product.Description,
                                Price = product.Price,
                                StockQuantity = product.StockQuantity
                            };
                            _context.Products.Add(newProduct);
                        }
                        await _context.SaveChangesAsync();
                    }
                    await transaction.CommitAsync();
                }
                catch (System.Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = $"Failed to register product and variants: {ex.Message}" });
                }
            }

            if (product.MultipleVariants != null && product.MultipleVariants.Any())
            {
                return Json(new { success = true, message = $"Product and {product.MultipleVariants.Count} variants registered successfully!" });
            }
            return Json(new { success = true, message = "Product created successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromBody] Product product)
        {
            if (id != product.Id)
            {
                return Json(new { success = false, message = "Product ID mismatch." });
            }

            // Verify main SKU uniqueness
            if (await _context.Products.AnyAsync(p => p.Sku == product.Sku && p.Id != id))
            {
                return Json(new { success = false, message = $"SKU '{product.Sku}' is already assigned to another product." });
            }

            if (product.MultipleVariants != null && product.MultipleVariants.Any())
            {
                // Manual validation check for multiple variants list
                foreach (var variant in product.MultipleVariants)
                {
                    if (string.IsNullOrWhiteSpace(variant.Sku))
                    {
                        return Json(new { success = false, message = "All variants must have a SKU." });
                    }
                    if (string.IsNullOrWhiteSpace(variant.Variant))
                    {
                        return Json(new { success = false, message = "All variants must have a Variant name." });
                    }
                    if (variant.Sku == product.Sku)
                    {
                        return Json(new { success = false, message = $"Variant SKU '{variant.Sku}' cannot be the same as the main SKU." });
                    }
                    if (await _context.Products.AnyAsync(p => p.Sku == variant.Sku))
                    {
                        return Json(new { success = false, message = $"SKU '{variant.Sku}' already exists." });
                    }
                }

                // Check list itself for duplicate SKUs
                var duplicateSkus = product.MultipleVariants.GroupBy(v => v.Sku).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                if (duplicateSkus.Any())
                {
                    return Json(new { success = false, message = $"Duplicate SKUs inside submission: {string.Join(", ", duplicateSkus)}" });
                }
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var existingProduct = await _context.Products.FindAsync(id);
                    if (existingProduct == null)
                    {
                        return Json(new { success = false, message = "Product not found." });
                    }

                    existingProduct.Sku = product.Sku;
                    existingProduct.Name = product.Name;
                    existingProduct.CategoryName = product.CategoryName;
                    existingProduct.ProductType = product.ProductType;
                    existingProduct.Variant = product.Variant;
                    existingProduct.Description = product.Description;

                    await _context.SaveChangesAsync();

                    // Save additional variants
                    if (product.MultipleVariants != null && product.MultipleVariants.Any())
                    {
                        foreach (var variant in product.MultipleVariants)
                        {
                            var newProduct = new Product
                            {
                                Sku = variant.Sku.Trim(),
                                Name = product.Name.Trim(),
                                Variant = variant.Variant.Trim(),
                                Description = product.Description,
                                Price = product.Price,
                                StockQuantity = product.StockQuantity
                            };
                            _context.Products.Add(newProduct);
                        }
                        await _context.SaveChangesAsync();
                    }
                    await transaction.CommitAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    if (!ProductExists(product.Id))
                    {
                        return Json(new { success = false, message = "Product not found." });
                    }
                    throw;
                }
                catch (System.Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = $"Failed to update product and variants: {ex.Message}" });
                }
            }

            if (product.MultipleVariants != null && product.MultipleVariants.Any())
            {
                return Json(new { success = true, message = $"Product updated and {product.MultipleVariants.Count} new variants added successfully!" });
            }
            return Json(new { success = true, message = "Product updated successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            // Check if there are purchases associated with this product
            bool hasPurchases = await _context.Purchases.AnyAsync(p => p.ProductId == id);
            // Check if there are sales associated with this product
            bool hasSales = await _context.Sales.AnyAsync(s => s.ProductId == id);

            if (hasPurchases || hasSales)
            {
                throw new BusinessException("Cannot delete product because it has purchase or sale transactions in history.");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Product deleted successfully!" });
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> VerifySku(string sku, int? id)
        {
            var exists = id.HasValue 
                ? await _context.Products.AnyAsync(p => p.Sku == sku && p.Id != id.Value)
                : await _context.Products.AnyAsync(p => p.Sku == sku);
            
            return Json(!exists);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
