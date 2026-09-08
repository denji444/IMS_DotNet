using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Exceptions;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PurchasesController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly IEmailSender _emailSender;

        public PurchasesController(InventoryDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            return View();
        }

        // DataTables Endpoint
        [HttpGet]
        public async Task<IActionResult> GetPurchasesData()
        {
            var purchases = await _context.Purchases
                .Include(p => p.Product)
                .Include(p => p.Supplier)
                .Select(p => new
                {
                    p.Id,
                    p.PurchaseNo,
                    ProductName = p.Product != null ? (string.IsNullOrEmpty(p.Product.Variant) ? $"{p.Product.Name} ({p.Product.Sku})" : $"{p.Product.Name} ({p.Product.Variant}) [{p.Product.Sku}]") : "N/A",
                    SupplierName = p.Supplier != null ? p.Supplier.Name : "N/A",
                    p.Quantity,
                    p.UnitPrice,
                    p.TotalCost,
                    PurchaseDate = p.PurchaseDate.ToString("yyyy-MM-dd HH:mm"),
                    p.Notes,
                    BatchNumber = string.IsNullOrEmpty(p.BatchNumber) ? "N/A" : p.BatchNumber,
                    PaymentMode = (int)p.PaymentMode
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
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (purchase == null)
            {
                return NotFound();
            }

            return Json(new
            {
                purchase.Id,
                purchase.PurchaseNo,
                purchase.ProductId,
                ProductName = purchase.Product != null ? (string.IsNullOrEmpty(purchase.Product.Variant) ? $"{purchase.Product.Name} ({purchase.Product.Sku})" : $"{purchase.Product.Name} ({purchase.Product.Variant}) [{purchase.Product.Sku}]") : null,
                purchase.SupplierId,
                SupplierName = purchase.Supplier?.Name,
                purchase.Quantity,
                purchase.UnitPrice,
                purchase.TotalCost,
                purchase.Notes,
                BatchNumber = purchase.BatchNumber ?? "",
                PaymentMode = (int)purchase.PaymentMode,
                purchase.DownPayment,
                purchase.InstallmentsCount,
                purchase.InstallmentFrequency
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableBatchesForProduct(int productId)
        {
            var purchases = await _context.Purchases
                .Where(p => p.ProductId == productId)
                .ToListAsync();

            var sales = await _context.Sales
                .Where(s => s.ProductId == productId)
                .ToListAsync();

            var batchGroups = purchases
                .GroupBy(p => string.IsNullOrWhiteSpace(p.BatchNumber) ? "Unbatched / General Stock" : p.BatchNumber.Trim())
                .Select(g => {
                    string batchName = g.Key;
                    int totalPurchased = g.Sum(p => p.Quantity);
                    int totalSold = sales
                        .Where(s => (string.IsNullOrWhiteSpace(s.BatchNumber) ? "Unbatched / General Stock" : s.BatchNumber.Trim()) == batchName)
                        .Sum(s => s.Quantity);
                    int availableQty = Math.Max(0, totalPurchased - totalSold);

                    return new
                    {
                        batchNumber = batchName == "Unbatched / General Stock" ? "" : batchName,
                        displayName = batchName == "Unbatched / General Stock" ? $"General / Unbatched Stock ({availableQty} available)" : $"{batchName} ({availableQty} available)",
                        availableQuantity = availableQty
                    };
                })
                .Where(b => b.availableQuantity > 0)
                .ToList();

            return Json(batchGroups);
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
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = $"Invalid data submitted: {errors}" });
            }

            var product = await _context.Products.FindAsync(purchase.ProductId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            // Generate unique PurchaseNo: PUR-yyyyMMdd-XXXX
            var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _context.Purchases.CountAsync(p => p.PurchaseNo.StartsWith($"PUR-{dateStr}")) + 1;
            purchase.PurchaseNo = $"PUR-{dateStr}-{count:D4}";

            purchase.TotalCost = purchase.Quantity * purchase.UnitPrice;
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

                    // Increment Product stock count
                    product.StockQuantity += purchase.Quantity;

                    _context.Purchases.Add(purchase);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Send email notification to Supplier
                    try
                    {
                        var supplier = await _context.Suppliers.FindAsync(purchase.SupplierId);
                        if (supplier != null && !string.IsNullOrEmpty(supplier.Email))
                        {
                            string emailBody = $@"
                                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
                                    <h2 style='color: #212529; border-bottom: 2px solid #212529; padding-bottom: 10px;'>Purchase Order Confirmation</h2>
                                    <p>Dear {supplier.ContactName},</p>
                                    <p>A new purchase stock-in order has been generated in our system for your reference:</p>
                                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                                        <tr style='background-color: #f8f9fa;'>
                                            <td style='padding: 8px; border: 1px solid #dee2e6; font-weight: bold;'>Order Number</td>
                                            <td style='padding: 8px; border: 1px solid #dee2e6;'>{purchase.PurchaseNo}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; border: 1px solid #dee2e6; font-weight: bold;'>Product</td>
                                            <td style='padding: 8px; border: 1px solid #dee2e6;'>{product.Name} (SKU: {product.Sku})</td>
                                        </tr>
                                        <tr style='background-color: #f8f9fa;'>
                                            <td style='padding: 8px; border: 1px solid #dee2e6; font-weight: bold;'>Quantity</td>
                                            <td style='padding: 8px; border: 1px solid #dee2e6;'>{purchase.Quantity}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; border: 1px solid #dee2e6; font-weight: bold;'>Unit Price</td>
                                            <td style='padding: 8px; border: 1px solid #dee2e6;'>PKR {purchase.UnitPrice:F2}</td>
                                        </tr>
                                        <tr style='background-color: #f8f9fa; font-weight: bold;'>
                                            <td style='padding: 8px; border: 1px solid #dee2e6; color: #212529;'>Total Cost</td>
                                            <td style='padding: 8px; border: 1px solid #dee2e6; color: #212529;'>PKR {purchase.TotalCost:F2}</td>
                                        </tr>
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
                        // Log SMTP exception, but don't prevent the success response since the database transaction succeeded!
                        // In a real application, you would log it.
                        Console.WriteLine($"SMTP delivery failed: {ex.Message}");
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
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            var existingPurchase = await _context.Purchases.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (existingPurchase == null)
            {
                return Json(new { success = false, message = "Purchase record not found." });
            }

            var product = await _context.Products.FindAsync(purchase.ProductId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            purchase.TotalCost = purchase.Quantity * purchase.UnitPrice;
            purchase.PurchaseNo = existingPurchase.PurchaseNo; // Keep original PurchaseNo
            purchase.PurchaseDate = existingPurchase.PurchaseDate; // Keep original date

            // Keep original lease configuration when editing
            purchase.PaymentMode = existingPurchase.PaymentMode;
            purchase.DownPayment = existingPurchase.DownPayment;
            purchase.InstallmentsCount = existingPurchase.InstallmentsCount;
            purchase.InstallmentFrequency = existingPurchase.InstallmentFrequency;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Adjust Product stock count: subtract old qty, add new qty
                    int stockDiff = purchase.Quantity - existingPurchase.Quantity;
                    if (product.StockQuantity + stockDiff < 0)
                    {
                        throw new BusinessException($"Cannot update purchase. Adjusting stock would make product stock negative (Current: {product.StockQuantity}, Change: {stockDiff}).");
                    }

                    product.StockQuantity += stockDiff;

                    _context.Entry(purchase).State = EntityState.Modified;
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
            var purchase = await _context.Purchases.FindAsync(id);
            if (purchase == null)
            {
                return Json(new { success = false, message = "Purchase record not found." });
            }

            var product = await _context.Products.FindAsync(purchase.ProductId);
            if (product == null)
            {
                return Json(new { success = false, message = "Associated product not found." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Decrement stock: ensure it doesn't go below zero
                    if (product.StockQuantity - purchase.Quantity < 0)
                    {
                        throw new BusinessException($"Cannot delete purchase. Doing so would reduce product stock below 0 (Stock: {product.StockQuantity}, Purchase Qty: {purchase.Quantity}).");
                    }

                    product.StockQuantity -= purchase.Quantity;

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
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (purchase == null)
            {
                return NotFound();
            }

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
                    pi.Status
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
            if (installment.PaidAmount == installment.Amount)
            {
                installment.Status = "Paid";
                installment.PaymentDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Payment recorded successfully!" });
        }

        public class PayInstallmentModel
        {
            public int InstallmentId { get; set; }
            public decimal Amount { get; set; }
        }
    }
}
