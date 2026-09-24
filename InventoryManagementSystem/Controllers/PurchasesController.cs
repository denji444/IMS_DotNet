using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Models.ViewModels;
using InventoryManagementSystem.Exceptions;
using InventoryManagementSystem.Services;

using Microsoft.Extensions.Logging;

namespace InventoryManagementSystem.Controllers
{
    [Authorize(Policy = "Purchases")]
    public class PurchasesController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PurchasesController> _logger;

        public PurchasesController(InventoryDbContext context, IEmailSender emailSender, ILogger<PurchasesController> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // DataTables Endpoint
        [HttpGet]
        public async Task<IActionResult> GetPurchasesData()
        {
            var purchases = await _context.Purchases.AsNoTracking()
                .Select(p => new
                {
                    p.Id,
                    p.PurchaseNo,
                    ProductName = p.Items.Any()
                        ? string.Join(", ", p.Items.Select(i => i.Product != null ? (string.IsNullOrEmpty(i.Product.Variant) ? i.Product.Name : $"{i.Product.Name} ({i.Product.Variant})") : "Item"))
                        : (p.Product != null ? p.Product.Name : "N/A"),
                    SupplierName = p.Supplier != null ? p.Supplier.Name : "N/A",
                    Quantity = p.Items.Any() ? p.Items.Sum(i => i.Quantity) : p.Quantity,
                    UnitPrice = p.Items.Any()
                        ? (p.Items.Count == 1 ? p.Items.First().UnitPrice : 0)
                        : p.UnitPrice,
                    p.TotalCost,
                    PurchaseDate = p.PurchaseDate.ToString("yyyy-MM-dd HH:mm"),
                    p.Notes,
                    BatchNumber = p.Items.Any(i => !string.IsNullOrEmpty(i.BatchNumber))
                        ? string.Join(", ", p.Items.Where(i => !string.IsNullOrEmpty(i.BatchNumber)).Select(i => i.BatchNumber))
                        : (string.IsNullOrEmpty(p.BatchNumber) ? "N/A" : p.BatchNumber),
                    PaymentMode = (int)p.PaymentMode,
                    PaymentMethod = (int)p.PaymentMethod,
                    PaymentReference = p.PaymentReference ?? "",
                    BankName = p.BankName ?? "",
                    ItemCount = p.Items.Count > 0 ? p.Items.Count : 1
                })
                .ToListAsync();

            return Json(new { data = purchases });
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchase(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Product)
                .Include(p => p.Supplier)
                .Include(p => p.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (purchase == null)
            {
                return NotFound();
            }

            var itemsList = purchase.Items.Select(i => new
            {
                i.Id,
                i.ProductId,
                ProductName = i.Product != null ? (string.IsNullOrEmpty(i.Product.Variant) ? $"{i.Product.Name} ({i.Product.Sku})" : $"{i.Product.Name} ({i.Product.Variant}) [{i.Product.Sku}]") : null,
                i.BatchNumber,
                barcode = i.Product != null ? i.Product.Barcode : null,
                i.Quantity,
                i.UnitPrice,
                i.DemandRate,
                i.FixRate,
                i.TotalCost
            }).ToList();

            if (!itemsList.Any() && purchase.ProductId.HasValue)
            {
                itemsList.Add(new
                {
                    Id = 0,
                    ProductId = purchase.ProductId.Value,
                    ProductName = purchase.Product != null ? (string.IsNullOrEmpty(purchase.Product.Variant) ? $"{purchase.Product.Name} ({purchase.Product.Sku})" : $"{purchase.Product.Name} ({purchase.Product.Variant}) [{purchase.Product.Sku}]") : null,
                    BatchNumber = purchase.BatchNumber,
                    barcode = purchase.Product != null ? purchase.Product.Barcode : null,
                    Quantity = purchase.Quantity,
                    UnitPrice = purchase.UnitPrice,
                    DemandRate = purchase.DemandRate,
                    FixRate = purchase.FixRate,
                    TotalCost = purchase.TotalCost
                });
            }

            return Json(new
            {
                purchase.Id,
                purchase.PurchaseNo,
                purchase.SupplierId,
                SupplierName = purchase.Supplier?.Name,
                purchase.TotalCost,
                purchase.Notes,
                PaymentMode = (int)purchase.PaymentMode,
                PaymentMethod = (int)purchase.PaymentMethod,
                PaymentReference = purchase.PaymentReference ?? "",
                BankName = purchase.BankName ?? "",
                CheckDate = purchase.CheckDate.HasValue ? purchase.CheckDate.Value.ToString("yyyy-MM-dd") : "",
                purchase.DownPayment,
                purchase.InstallmentsCount,
                purchase.InstallmentFrequency,
                Items = itemsList
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableBatchesForProduct(int? productId, bool includeAll = true)
        {
            if (!productId.HasValue || productId.Value <= 0)
            {
                var allPurchaseBatches = await _context.PurchaseItems
                    .Where(pi => !string.IsNullOrWhiteSpace(pi.BatchNumber))
                    .Select(pi => (pi.BatchNumber ?? "").Trim())
                    .Distinct()
                    .ToListAsync();

                var legacyPurchaseBatches = await _context.Purchases
                    .Where(p => !string.IsNullOrWhiteSpace(p.BatchNumber) && !p.Items.Any())
                    .Select(p => (p.BatchNumber ?? "").Trim())
                    .Distinct()
                    .ToListAsync();

                var allBatches = allPurchaseBatches.Concat(legacyPurchaseBatches)
                    .Where(b => !string.IsNullOrWhiteSpace(b))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(b => new
                    {
                        batchNumber = b,
                        displayName = b,
                        availableQuantity = 0,
                        purchaseRate = 0m,
                        demandRate = (decimal?)null,
                        fixRate = (decimal?)null
                    })
                    .ToList();

                return Json(allBatches);
            }

            int id = productId.Value;
            var purchaseItems = await _context.PurchaseItems
                .Where(pi => pi.ProductId == id)
                .Select(pi => new
                {
                    pi.BatchNumber,
                    pi.Quantity,
                    pi.UnitPrice,
                    pi.DemandRate,
                    pi.FixRate
                })
                .ToListAsync();

            var legacyPurchases = await _context.Purchases
                .Where(p => p.ProductId == id && !p.Items.Any())
                .Select(p => new
                {
                    p.BatchNumber,
                    p.Quantity,
                    UnitPrice = p.UnitPrice,
                    DemandRate = p.DemandRate,
                    FixRate = p.FixRate
                })
                .ToListAsync();

            var allPurchases = purchaseItems.Concat(legacyPurchases).ToList();

            var saleItems = await _context.SaleItems
                .Where(si => si.ProductId == id)
                .Select(si => new { si.BatchNumber, si.Quantity })
                .ToListAsync();

            var legacySales = await _context.Sales
                .Where(s => s.ProductId == id && !s.Items.Any())
                .Select(s => new { s.BatchNumber, s.Quantity })
                .ToListAsync();

            var allSales = saleItems.Concat(legacySales).ToList();

            var batchGroupsQuery = allPurchases
                .GroupBy(p => string.IsNullOrWhiteSpace(p.BatchNumber) ? "Unbatched / General Stock" : p.BatchNumber.Trim())
                .Select(g => {
                    string batchName = g.Key;
                    int totalPurchased = g.Sum(p => p.Quantity);
                    int totalSold = allSales
                        .Where(s => (string.IsNullOrWhiteSpace(s.BatchNumber) ? "Unbatched / General Stock" : s.BatchNumber.Trim()) == batchName)
                        .Sum(s => s.Quantity);
                    int availableQty = Math.Max(0, totalPurchased - totalSold);

                    var latestItem = g.LastOrDefault();
                    decimal purchaseRate = latestItem != null ? latestItem.UnitPrice : 0;
                    decimal? demandRate = latestItem?.DemandRate;
                    decimal? fixRate = latestItem?.FixRate;

                    return new
                    {
                        batchNumber = batchName == "Unbatched / General Stock" ? "" : batchName,
                        displayName = batchName == "Unbatched / General Stock" 
                            ? $"General / Unbatched Stock ({availableQty} available)" 
                            : $"{batchName} ({availableQty} available)",
                        availableQuantity = availableQty,
                        purchaseRate = purchaseRate,
                        demandRate = demandRate,
                        fixRate = fixRate
                    };
                });

            if (!includeAll)
            {
                batchGroupsQuery = batchGroupsQuery.Where(b => b.availableQuantity > 0);
            }

            return Json(batchGroupsQuery.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] Purchase purchase)
        {
            if (purchase.NewSupplier != null)
            {
                purchase.NewSupplier.ContactName = purchase.NewSupplier.Name;
                ModelState.Remove("NewSupplier.ContactName");
                ModelState.Remove(nameof(purchase.SupplierId));

                if (string.IsNullOrWhiteSpace(purchase.NewSupplier.Name))
                    ModelState.AddModelError("NewSupplier.Name", "Supplier Name is required.");
                if (string.IsNullOrWhiteSpace(purchase.NewSupplier.Email))
                    ModelState.AddModelError("NewSupplier.Email", "Email is required.");
                if (string.IsNullOrWhiteSpace(purchase.NewSupplier.Phone))
                    ModelState.AddModelError("NewSupplier.Phone", "Phone is required.");
                if (string.IsNullOrWhiteSpace(purchase.NewSupplier.Address))
                    ModelState.AddModelError("NewSupplier.Address", "Address is required.");

                // Validate uniqueness of name
                if (!string.IsNullOrWhiteSpace(purchase.NewSupplier.Name) &&
                    await _context.Suppliers.AnyAsync(s => s.Name == purchase.NewSupplier.Name))
                {
                    ModelState.AddModelError("NewSupplier.Name", $"Supplier Name '{purchase.NewSupplier.Name}' is already in use.");
                }
            }

            ModelState.Remove(nameof(purchase.PurchaseNo));
            ModelState.Remove(nameof(purchase.ProductId));
            ModelState.Remove(nameof(purchase.Quantity));
            ModelState.Remove(nameof(purchase.UnitPrice));

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = $"Invalid data submitted: {errors}" });
            }

            if (purchase.Items == null || !purchase.Items.Any())
            {
                if (purchase.ProductId.HasValue && purchase.Quantity > 0)
                {
                    purchase.Items = new System.Collections.Generic.List<PurchaseItem>
                    {
                        new PurchaseItem
                        {
                            ProductId = purchase.ProductId.Value,
                            BatchNumber = purchase.BatchNumber,
                            Quantity = purchase.Quantity,
                            UnitPrice = purchase.UnitPrice,
                            TotalCost = purchase.Quantity * purchase.UnitPrice
                        }
                    };
                }
                else
                {
                    return Json(new { success = false, message = "At least one item is required for the purchase order." });
                }
            }

            // Validate all items
            foreach (var item in purchase.Items)
            {
                var prod = await _context.Products.FindAsync(item.ProductId);
                if (prod == null)
                {
                    return Json(new { success = false, message = $"Product with ID {item.ProductId} not found." });
                }
                if (item.FixRate.HasValue && item.FixRate.Value < item.UnitPrice)
                {
                    return Json(new { success = false, message = $"Fix Rate (PKR {item.FixRate.Value:F2}) for '{prod.Name}' cannot be less than Purchase Rate (PKR {item.UnitPrice:F2})." });
                }
                if (item.DemandRate.HasValue && item.FixRate.HasValue && item.DemandRate.Value < item.FixRate.Value)
                {
                    return Json(new { success = false, message = $"Demand Rate (PKR {item.DemandRate.Value:F2}) for '{prod.Name}' cannot be less than Fix Rate (PKR {item.FixRate.Value:F2})." });
                }
                if (item.DemandRate.HasValue && !item.FixRate.HasValue && item.DemandRate.Value < item.UnitPrice)
                {
                    return Json(new { success = false, message = $"Demand Rate (PKR {item.DemandRate.Value:F2}) for '{prod.Name}' cannot be less than Purchase Rate (PKR {item.UnitPrice:F2})." });
                }
                item.TotalCost = item.Quantity * item.UnitPrice;
            }

            // Generate unique PurchaseNo: PINV-yyyyMMdd-XXXX (Purchase Invoice)
            var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _context.Purchases.CountAsync(p => p.PurchaseNo.StartsWith($"PINV-{dateStr}") || p.PurchaseNo.StartsWith($"PUR-{dateStr}")) + 1;
            purchase.PurchaseNo = $"PINV-{dateStr}-{count:D4}";

            purchase.TotalCost = purchase.Items.Sum(i => i.TotalCost);
            purchase.Quantity = purchase.Items.Sum(i => i.Quantity);
            purchase.ProductId = purchase.Items.First().ProductId;
            purchase.UnitPrice = purchase.Items.Count == 1 ? purchase.Items.First().UnitPrice : 0;
            purchase.BatchNumber = purchase.Items.First().BatchNumber;
            purchase.DemandRate = purchase.Items.First().DemandRate;
            purchase.FixRate = purchase.Items.First().FixRate;
            purchase.PurchaseDate = DateTime.UtcNow;

            if (purchase.PaymentMode == PaymentMode.Lease)
            {
                if (purchase.DownPayment < 0)
                {
                    return Json(new { success = false, message = "Down payment cannot be negative." });
                }
                if (purchase.DownPayment >= purchase.TotalCost)
                {
                    return Json(new { success = false, message = "Down payment must be less than the total cost." });
                }
                if (purchase.InstallmentsCount < 1)
                {
                    return Json(new { success = false, message = "Installment count must be at least 1." });
                }

                decimal remainingBalance = purchase.TotalCost - purchase.DownPayment;
                decimal installmentAmount = Math.Round(remainingBalance / purchase.InstallmentsCount, 2);

                // Adjust for rounding differences on the last installment
                decimal sumInstallments = installmentAmount * purchase.InstallmentsCount;
                decimal difference = remainingBalance - sumInstallments;

                purchase.Installments = new System.Collections.Generic.List<PurchaseInstallment>();
                for (int i = 1; i <= purchase.InstallmentsCount; i++)
                {
                    decimal currentAmount = installmentAmount;
                    if (i == purchase.InstallmentsCount)
                    {
                        currentAmount += difference;
                    }

                    DateTime dueDate = purchase.PurchaseDate;
                    if (purchase.InstallmentFrequency == "Weekly")
                    {
                        dueDate = dueDate.AddDays(7 * i);
                    }
                    else
                    {
                        dueDate = dueDate.AddMonths(i);
                    }

                    purchase.Installments.Add(new PurchaseInstallment
                    {
                        InstallmentNumber = i,
                        Amount = currentAmount,
                        PaidAmount = 0.00m,
                        DueDate = dueDate,
                        Status = "Pending"
                    });
                }
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (purchase.NewSupplier != null)
                    {
                        // Bypass verification for inline supplier, directly register as active
                        purchase.NewSupplier.IsEmailVerified = true;
                        purchase.NewSupplier.EmailVerificationToken = null;
                        // Map ContactName to Name to satisfy required DB constraint
                        purchase.NewSupplier.ContactName = purchase.NewSupplier.Name;

                        _context.Suppliers.Add(purchase.NewSupplier);
                        await _context.SaveChangesAsync();

                        purchase.SupplierId = purchase.NewSupplier.Id;
                    }

                    // Increment Product stock count for all items and assign new Barcode if provided
                    foreach (var item in purchase.Items)
                    {
                        var prod = await _context.Products.FindAsync(item.ProductId);
                        if (prod != null)
                        {
                            prod.StockQuantity += item.Quantity;
                            if (!string.IsNullOrWhiteSpace(item.Barcode))
                            {
                                var cleanBc = item.Barcode.Trim();
                                var existsOther = await _context.Products.AnyAsync(p => p.Id != prod.Id && p.Barcode != null && p.Barcode.ToLower() == cleanBc.ToLower());
                                if (!existsOther)
                                {
                                    prod.Barcode = cleanBc;
                                }
                            }
                        }
                    }

                    _context.Purchases.Add(purchase);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Send email notification to Supplier
                    try
                    {
                        var supplier = await _context.Suppliers.FindAsync(purchase.SupplierId);
                        if (supplier != null && !string.IsNullOrEmpty(supplier.Email))
                        {
                            string itemsTableRows = "";
                            if (purchase.Items != null && purchase.Items.Any())
                            {
                                foreach (var item in purchase.Items)
                                {
                                    var prod = await _context.Products.FindAsync(item.ProductId);
                                    string pName = prod != null ? (string.IsNullOrEmpty(prod.Variant) ? prod.Name : $"{prod.Name} ({prod.Variant})") : "Item";
                                    itemsTableRows += $"<tr><td style='padding: 6px; border: 1px solid #dee2e6;'>{pName}</td><td style='padding: 6px; border: 1px solid #dee2e6; text-align: center;'>{item.Quantity}</td><td style='padding: 6px; border: 1px solid #dee2e6; text-align: right;'>PKR {item.UnitPrice:F2}</td><td style='padding: 6px; border: 1px solid #dee2e6; text-align: right;'>PKR {item.TotalCost:F2}</td></tr>";
                                }
                            }

                            string emailBody = $@"
                                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
                                    <h2 style='color: #212529; border-bottom: 2px solid #212529; padding-bottom: 10px;'>Purchase Order Confirmation</h2>
                                    <p>Dear {supplier.ContactName},</p>
                                    <p>A new purchase stock-in order ({purchase.PurchaseNo}) has been generated in our system for your reference:</p>
                                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                                        <thead style='background-color: #212529; color: #fff;'>
                                            <tr>
                                                <th style='padding: 8px; border: 1px solid #dee2e6; text-align: left;'>Product</th>
                                                <th style='padding: 8px; border: 1px solid #dee2e6; text-align: center;'>Qty</th>
                                                <th style='padding: 8px; border: 1px solid #dee2e6; text-align: right;'>Unit Cost</th>
                                                <th style='padding: 8px; border: 1px solid #dee2e6; text-align: right;'>Total</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {itemsTableRows}
                                        </tbody>
                                        <tfoot>
                                            <tr style='background-color: #f8f9fa; font-weight: bold;'>
                                                <td colspan='3' style='padding: 8px; border: 1px solid #dee2e6; text-align: right;'>Grand Total Cost</td>
                                                <td style='padding: 8px; border: 1px solid #dee2e6; text-align: right; color: #212529;'>PKR {purchase.TotalCost:F2}</td>
                                            </tr>
                                        </tfoot>
                                    </table>
                                    {(string.IsNullOrEmpty(purchase.Notes) ? "" : $"<p><strong>Notes:</strong> {purchase.Notes}</p>")}
                                    <p>Please prepare the shipment if not already delivered. Contact our procurement team if you have any questions.</p>
                                    <p>Best regards,<br/><strong>IMS Procurement Team</strong></p>
                                </div>";

                            await _emailSender.SendEmailAsync(supplier.Email, $"New Purchase Order {purchase.PurchaseNo} - IMS", emailBody);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "SMTP delivery failed for Purchase Order {PurchaseNo}", purchase.PurchaseNo);
                    }

                    return Json(new { success = true, message = "Purchase stock-in recorded successfully!" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new BusinessException($"Failed to save purchase: {ex.Message}");
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromBody] Purchase purchase)
        {
            if (id != purchase.Id)
            {
                return Json(new { success = false, message = "Purchase ID mismatch." });
            }

            ModelState.Remove(nameof(purchase.PurchaseNo));
            ModelState.Remove(nameof(purchase.ProductId));
            ModelState.Remove(nameof(purchase.Quantity));
            ModelState.Remove(nameof(purchase.UnitPrice));

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            var existingPurchase = await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingPurchase == null)
            {
                return Json(new { success = false, message = "Purchase record not found." });
            }

            if (purchase.Items == null || !purchase.Items.Any())
            {
                return Json(new { success = false, message = "At least one item is required for the purchase order." });
            }

            // Validate all new items
            foreach (var item in purchase.Items)
            {
                var prod = await _context.Products.FindAsync(item.ProductId);
                if (prod == null)
                {
                    return Json(new { success = false, message = $"Product with ID {item.ProductId} not found." });
                }
                if (item.FixRate.HasValue && item.FixRate.Value < item.UnitPrice)
                {
                    return Json(new { success = false, message = $"Fix Rate (PKR {item.FixRate.Value:F2}) for '{prod.Name}' cannot be less than Purchase Rate (PKR {item.UnitPrice:F2})." });
                }
                if (item.DemandRate.HasValue && item.FixRate.HasValue && item.DemandRate.Value < item.FixRate.Value)
                {
                    return Json(new { success = false, message = $"Demand Rate (PKR {item.DemandRate.Value:F2}) for '{prod.Name}' cannot be less than Fix Rate (PKR {item.FixRate.Value:F2})." });
                }
                if (item.DemandRate.HasValue && !item.FixRate.HasValue && item.DemandRate.Value < item.UnitPrice)
                {
                    return Json(new { success = false, message = $"Demand Rate (PKR {item.DemandRate.Value:F2}) for '{prod.Name}' cannot be less than Purchase Rate (PKR {item.UnitPrice:F2})." });
                }
                item.TotalCost = item.Quantity * item.UnitPrice;
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Revert stock of existing purchase items
                    if (existingPurchase.Items != null && existingPurchase.Items.Any())
                    {
                        foreach (var oldItem in existingPurchase.Items)
                        {
                            var prod = await _context.Products.FindAsync(oldItem.ProductId);
                            if (prod != null)
                            {
                                if (prod.StockQuantity - oldItem.Quantity < 0)
                                {
                                    throw new BusinessException($"Cannot update purchase. Doing so would reduce product stock for '{prod.Name}' below 0.");
                                }
                                prod.StockQuantity -= oldItem.Quantity;
                            }
                        }
                        _context.PurchaseItems.RemoveRange(existingPurchase.Items);
                    }
                    else if (existingPurchase.ProductId.HasValue)
                    {
                        var prod = await _context.Products.FindAsync(existingPurchase.ProductId.Value);
                        if (prod != null)
                        {
                            if (prod.StockQuantity - existingPurchase.Quantity < 0)
                            {
                                throw new BusinessException($"Cannot update purchase. Doing so would reduce product stock below 0.");
                            }
                            prod.StockQuantity -= existingPurchase.Quantity;
                        }
                    }

                    // 2. Add stock for new items and assign new Barcode if provided
                    foreach (var newItem in purchase.Items)
                    {
                        var prod = await _context.Products.FindAsync(newItem.ProductId);
                        if (prod != null)
                        {
                            prod.StockQuantity += newItem.Quantity;
                            if (!string.IsNullOrWhiteSpace(newItem.Barcode))
                            {
                                var cleanBc = newItem.Barcode.Trim();
                                var existsOther = await _context.Products.AnyAsync(p => p.Id != prod.Id && p.Barcode != null && p.Barcode.ToLower() == cleanBc.ToLower());
                                if (!existsOther)
                                {
                                    prod.Barcode = cleanBc;
                                }
                            }
                        }
                    }

                    // Update properties on existingPurchase
                    existingPurchase.SupplierId = purchase.SupplierId;
                    existingPurchase.Notes = purchase.Notes;
                    existingPurchase.PaymentMethod = purchase.PaymentMethod;
                    existingPurchase.PaymentReference = purchase.PaymentReference;
                    existingPurchase.BankName = purchase.BankName;
                    existingPurchase.CheckDate = purchase.CheckDate;

                    existingPurchase.TotalCost = purchase.Items.Sum(i => i.TotalCost);
                    existingPurchase.Quantity = purchase.Items.Sum(i => i.Quantity);
                    existingPurchase.ProductId = purchase.Items.First().ProductId;
                    existingPurchase.UnitPrice = purchase.Items.Count == 1 ? purchase.Items.First().UnitPrice : 0;
                    existingPurchase.BatchNumber = purchase.Items.First().BatchNumber;
                    existingPurchase.DemandRate = purchase.Items.First().DemandRate;
                    existingPurchase.FixRate = purchase.Items.First().FixRate;

                    foreach (var item in purchase.Items)
                    {
                        item.PurchaseId = id;
                        _context.PurchaseItems.Add(item);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Purchase updated successfully!" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    if (ex is BusinessException) throw;
                    throw new BusinessException($"Failed to update purchase: {ex.Message}");
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null)
            {
                return Json(new { success = false, message = "Purchase record not found." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Decrement stock for all items
                    if (purchase.Items != null && purchase.Items.Any())
                    {
                        foreach (var item in purchase.Items)
                        {
                            var prod = await _context.Products.FindAsync(item.ProductId);
                            if (prod != null)
                            {
                                if (prod.StockQuantity - item.Quantity < 0)
                                {
                                    throw new BusinessException($"Cannot delete purchase. Doing so would reduce product stock for '{prod.Name}' below 0.");
                                }
                                prod.StockQuantity -= item.Quantity;
                            }
                        }
                    }
                    else if (purchase.ProductId.HasValue)
                    {
                        var prod = await _context.Products.FindAsync(purchase.ProductId.Value);
                        if (prod != null)
                        {
                            if (prod.StockQuantity - purchase.Quantity < 0)
                            {
                                throw new BusinessException($"Cannot delete purchase. Doing so would reduce product stock below 0.");
                            }
                            prod.StockQuantity -= purchase.Quantity;
                        }
                    }

                    _context.Purchases.Remove(purchase);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Purchase deleted successfully!" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    if (ex is BusinessException) throw;
                    throw new BusinessException($"Failed to delete purchase: {ex.Message}");
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Product)
                .Include(p => p.Supplier)
                .Include(p => p.Installments)
                .Include(p => p.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (purchase == null)
            {
                return NotFound();
            }

            ViewData["CompanyProfile"] = await _context.CompanyProfiles.FirstOrDefaultAsync();
            return View(purchase);
        }

        [HttpGet]
        public async Task<IActionResult> GetInstallments(int purchaseId)
        {
            var installments = await _context.PurchaseInstallments
                .Where(pi => pi.PurchaseId == purchaseId)
                .OrderBy(pi => pi.InstallmentNumber)
                .Select(pi => new
                {
                    pi.Id,
                    pi.InstallmentNumber,
                    pi.Amount,
                    pi.PaidAmount,
                    DueDate = pi.DueDate.ToString("yyyy-MM-dd"),
                    PaymentDate = pi.PaymentDate.HasValue ? pi.PaymentDate.Value.ToString("yyyy-MM-dd HH:mm") : "-",
                    pi.Status,
                    PaymentMethod = pi.PaymentMethod.HasValue ? (int)pi.PaymentMethod.Value : (int?)null,
                    PaymentMethodName = pi.PaymentMethod.HasValue ? pi.PaymentMethod.Value.ToString() : null,
                    PaymentReference = pi.PaymentReference ?? "",
                    BankName = pi.BankName ?? "",
                    CheckDate = pi.CheckDate.HasValue ? pi.CheckDate.Value.ToString("yyyy-MM-dd") : ""
                })
                .ToListAsync();

            return Json(new { success = true, data = installments });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayInstallment([FromBody] PayInstallmentModel model)
        {
            if (model == null || model.InstallmentId <= 0 || model.Amount <= 0)
            {
                return Json(new { success = false, message = "Invalid payment data." });
            }

            var installment = await _context.PurchaseInstallments
                .Include(pi => pi.Purchase)
                .FirstOrDefaultAsync(pi => pi.Id == model.InstallmentId);

            if (installment == null)
            {
                return Json(new { success = false, message = "Installment not found." });
            }

            if (installment.Status == "Paid")
            {
                return Json(new { success = false, message = "This installment is already fully paid." });
            }

            decimal remainingToPay = installment.Amount - installment.PaidAmount;
            if (model.Amount > remainingToPay)
            {
                return Json(new { success = false, message = $"Payment amount exceeds the remaining installment balance of PKR {remainingToPay:F2}." });
            }

            installment.PaidAmount += model.Amount;
            installment.PaymentMethod = model.PaymentMethod;
            installment.PaymentReference = model.PaymentReference;
            installment.BankName = model.BankName;
            installment.CheckDate = model.CheckDate;

            if (installment.PaidAmount == installment.Amount)
            {
                installment.Status = "Paid";
                installment.PaymentDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Payment recorded successfully!" });
        }
    }
}
