using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [Authorize(Policy = "Sales")]
    public class SalesController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<SalesController> _logger;

        public SalesController(
            InventoryDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<SalesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // DataTables Endpoint
        [HttpGet]
        public async Task<IActionResult> GetSalesData()
        {
            var sales = await _context.Sales.AsNoTracking()
                .Select(s => new
                {
                    s.Id,
                    s.InvoiceNo,
                    ProductName = s.Items.Any()
                        ? string.Join(", ", s.Items.Select(i => i.Product != null ? (string.IsNullOrEmpty(i.Product.Variant) ? i.Product.Name : $"{i.Product.Name} ({i.Product.Variant})") : "Item"))
                        : (s.Product != null ? s.Product.Name : "N/A"),
                    CustomerName = s.Customer != null ? s.Customer.FullName : "N/A",
                    Quantity = s.Items.Any() ? s.Items.Sum(i => i.Quantity) : s.Quantity,
                    UnitPrice = s.Items.Any()
                        ? (s.Items.Count == 1 ? s.Items.First().UnitPrice : 0)
                        : s.UnitPrice,
                    s.TotalAmount,
                    SaleDate = s.SaleDate.ToString("yyyy-MM-dd HH:mm"),
                    s.Notes,
                    BatchNumber = s.Items.Any(i => !string.IsNullOrEmpty(i.BatchNumber))
                        ? string.Join(", ", s.Items.Where(i => !string.IsNullOrEmpty(i.BatchNumber)).Select(i => i.BatchNumber))
                        : (string.IsNullOrEmpty(s.BatchNumber) ? "N/A" : s.BatchNumber),
                    PaymentMode = (int)s.PaymentMode,
                    PaymentMethod = (int)s.PaymentMethod,
                    PaymentReference = s.PaymentReference ?? "",
                    BankName = s.BankName ?? "",
                    ItemCount = s.Items.Count > 0 ? s.Items.Count : 1
                })
                .ToListAsync();

            return Json(new { data = sales });
        }

        [HttpGet]
        public async Task<IActionResult> GetSale(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.Product)
                .Include(s => s.Customer)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (sale == null)
            {
                return NotFound();
            }

            var itemsList = sale.Items.Select(i => new
            {
                i.Id,
                i.ProductId,
                ProductName = i.Product != null ? (string.IsNullOrEmpty(i.Product.Variant) ? $"{i.Product.Name} ({i.Product.Sku})" : $"{i.Product.Name} ({i.Product.Variant}) [{i.Product.Sku}]") : null,
                i.BatchNumber,
                i.Quantity,
                i.UnitPrice,
                i.TotalAmount
            }).ToList();

            if (!itemsList.Any() && sale.ProductId.HasValue)
            {
                itemsList.Add(new
                {
                    Id = 0,
                    ProductId = sale.ProductId.Value,
                    ProductName = sale.Product != null ? (string.IsNullOrEmpty(sale.Product.Variant) ? $"{sale.Product.Name} ({sale.Product.Sku})" : $"{sale.Product.Name} ({sale.Product.Variant}) [{sale.Product.Sku}]") : null,
                    BatchNumber = sale.BatchNumber,
                    Quantity = sale.Quantity,
                    UnitPrice = sale.UnitPrice,
                    TotalAmount = sale.TotalAmount
                });
            }

            return Json(new
            {
                sale.Id,
                sale.InvoiceNo,
                sale.CustomerId,
                CustomerName = sale.Customer?.FullName,
                sale.TotalAmount,
                sale.Notes,
                PaymentMode = (int)sale.PaymentMode,
                PaymentMethod = (int)sale.PaymentMethod,
                PaymentReference = sale.PaymentReference ?? "",
                BankName = sale.BankName ?? "",
                CheckDate = sale.CheckDate.HasValue ? sale.CheckDate.Value.ToString("yyyy-MM-dd") : "",
                sale.DownPayment,
                sale.InstallmentsCount,
                sale.InstallmentFrequency,
                Items = itemsList
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] Sale sale)
        {
            ApplicationUser? customer = null;
            if (!string.IsNullOrWhiteSpace(sale.NewCustomerEmail))
            {
                // Remove CustomerId from validation since it will be generated inline
                ModelState.Remove(nameof(sale.CustomerId));

                // Perform manual validation for inline customer fields
                if (string.IsNullOrWhiteSpace(sale.NewCustomerFirstName))
                    ModelState.AddModelError("NewCustomerFirstName", "First Name is required.");
                if (string.IsNullOrWhiteSpace(sale.NewCustomerLastName))
                    ModelState.AddModelError("NewCustomerLastName", "Last Name is required.");
                if (string.IsNullOrWhiteSpace(sale.NewCustomerPhone))
                    ModelState.AddModelError("NewCustomerPhone", "Phone Number is required.");

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(sale.NewCustomerEmail);
                if (existingUser != null)
                {
                    ModelState.AddModelError("NewCustomerEmail", $"Email '{sale.NewCustomerEmail}' is already registered.");
                }
            }

            ModelState.Remove(nameof(sale.InvoiceNo));
            ModelState.Remove(nameof(sale.ProductId));
            ModelState.Remove(nameof(sale.Quantity));
            ModelState.Remove(nameof(sale.UnitPrice));

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = $"Invalid data submitted: {errors}" });
            }

            if (sale.Items == null || !sale.Items.Any())
            {
                if (sale.ProductId.HasValue && sale.Quantity > 0)
                {
                    sale.Items = new System.Collections.Generic.List<SaleItem>
                    {
                        new SaleItem
                        {
                            ProductId = sale.ProductId.Value,
                            BatchNumber = sale.BatchNumber,
                            Quantity = sale.Quantity,
                            UnitPrice = sale.UnitPrice,
                            TotalAmount = sale.Quantity * sale.UnitPrice
                        }
                    };
                }
                else
                {
                    return Json(new { success = false, message = "At least one item is required for the sale invoice." });
                }
            }

            // Validate all items in invoice against batch FixRate threshold
            foreach (var item in sale.Items)
            {
                var prod = await _context.Products.FindAsync(item.ProductId);
                if (prod == null)
                {
                    return Json(new { success = false, message = $"Product with ID {item.ProductId} not found." });
                }

                // Look up FixRate for this specific batch
                decimal? fixRate = null;
                if (!string.IsNullOrWhiteSpace(item.BatchNumber))
                {
                    fixRate = await _context.PurchaseItems
                        .Where(pi => pi.ProductId == item.ProductId && pi.BatchNumber == item.BatchNumber && pi.FixRate.HasValue)
                        .OrderBy(pi => pi.Id)
                        .Select(pi => pi.FixRate)
                        .LastOrDefaultAsync();

                    if (!fixRate.HasValue)
                    {
                        fixRate = await _context.Purchases
                            .Where(p => p.ProductId == item.ProductId && p.BatchNumber == item.BatchNumber && p.FixRate.HasValue)
                            .OrderBy(p => p.Id)
                            .Select(p => p.FixRate)
                            .LastOrDefaultAsync();
                    }
                }

                if (fixRate.HasValue && item.UnitPrice < fixRate.Value)
                {
                    return Json(new { success = false, message = $"Unit price for '{prod.Name}' (PKR {item.UnitPrice:F2}) cannot be less than the minimum Fix Rate threshold (PKR {fixRate.Value:F2}) for batch '{item.BatchNumber}'." });
                }
                else if (!fixRate.HasValue && prod.Price.HasValue && item.UnitPrice < prod.Price.Value)
                {
                    return Json(new { success = false, message = $"Unit price for '{prod.Name}' cannot be less than set price (PKR {prod.Price.Value:F2})." });
                }

                item.TotalAmount = item.Quantity * item.UnitPrice;
            }

            // Aggregate quantities by product and check stock
            var productGroupedQty = sale.Items
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(x => x.Quantity) })
                .ToList();

            foreach (var group in productGroupedQty)
            {
                var prod = await _context.Products.FindAsync(group.ProductId);
                if (prod!.StockQuantity < group.TotalQty)
                {
                    throw new BusinessException($"Insufficient stock for '{prod.Name}'. Requested stock out: {group.TotalQty}, Available in stock: {prod.StockQuantity}");
                }
            }

            // Generate InvoiceNo: INV-yyyyMMdd-XXXX
            var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _context.Sales.CountAsync(s => s.InvoiceNo.StartsWith($"INV-{dateStr}")) + 1;
            sale.InvoiceNo = $"INV-{dateStr}-{count:D4}";

            sale.TotalAmount = sale.Items.Sum(i => i.TotalAmount);
            sale.Quantity = sale.Items.Sum(i => i.Quantity);
            sale.ProductId = sale.Items.First().ProductId;
            sale.UnitPrice = sale.Items.Count == 1 ? sale.Items.First().UnitPrice : 0;
            sale.BatchNumber = sale.Items.First().BatchNumber;
            sale.SaleDate = DateTime.UtcNow;

            if (sale.PaymentMode == PaymentMode.Lease)
            {
                if (sale.DownPayment < 0)
                {
                    return Json(new { success = false, message = "Down payment cannot be negative." });
                }
                if (sale.DownPayment >= sale.TotalAmount)
                {
                    return Json(new { success = false, message = "Down payment must be less than the total sale amount." });
                }
                if (sale.InstallmentsCount < 1)
                {
                    return Json(new { success = false, message = "Installment count must be at least 1." });
                }

                decimal remainingBalance = sale.TotalAmount - sale.DownPayment;
                decimal installmentAmount = Math.Round(remainingBalance / sale.InstallmentsCount, 2);

                // Adjust for rounding differences on the last installment
                decimal sumInstallments = installmentAmount * sale.InstallmentsCount;
                decimal difference = remainingBalance - sumInstallments;

                sale.Installments = new System.Collections.Generic.List<SaleInstallment>();
                for (int i = 1; i <= sale.InstallmentsCount; i++)
                {
                    decimal currentAmount = installmentAmount;
                    if (i == sale.InstallmentsCount)
                    {
                        currentAmount += difference;
                    }

                    DateTime dueDate = sale.SaleDate;
                    if (sale.InstallmentFrequency == "Weekly")
                    {
                        dueDate = dueDate.AddDays(7 * i);
                    }
                    else
                    {
                        dueDate = dueDate.AddMonths(i);
                    }

                    sale.Installments.Add(new SaleInstallment
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
                    if (!string.IsNullOrWhiteSpace(sale.NewCustomerEmail))
                    {
                        customer = new ApplicationUser
                        {
                            UserName = sale.NewCustomerEmail,
                            Email = sale.NewCustomerEmail,
                            FullName = $"{sale.NewCustomerFirstName} {sale.NewCustomerLastName}".Trim(),
                            PhoneNumber = sale.NewCustomerPhone,
                            Cnic = sale.NewCustomerCnic,
                            CreatedAt = DateTime.UtcNow,
                            EmailConfirmed = true // auto-confirm email since dashboard is not needed
                        };

                        var randomPassword = Guid.NewGuid().ToString() + "aA1!";
                        var createResult = await _userManager.CreateAsync(customer, randomPassword);
                        if (!createResult.Succeeded)
                        {
                            string regErrors = string.Join(" ", createResult.Errors.Select(e => e.Description));
                            throw new BusinessException($"Failed to register inline customer: {regErrors}");
                        }

                        await _userManager.AddToRoleAsync(customer, "Customer");
                        sale.CustomerId = customer.Id;
                    }
                    else
                    {
                        customer = await _userManager.FindByIdAsync(sale.CustomerId);
                    }

                    // Decrement stock for all items
                    foreach (var group in productGroupedQty)
                    {
                        var prod = await _context.Products.FindAsync(group.ProductId);
                        prod!.StockQuantity -= group.TotalQty;
                    }

                    _context.Sales.Add(sale);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new BusinessException($"Failed to create sale: {ex.Message}");
                }
            }

            // Send sales invoice confirmation email to Customer
            try
            {
                if (customer != null && !string.IsNullOrEmpty(customer.Email))
                {
                    string itemsTableRows = "";
                    if (sale.Items != null && sale.Items.Any())
                    {
                        foreach (var item in sale.Items)
                        {
                            var prod = await _context.Products.FindAsync(item.ProductId);
                            string pName = prod != null ? (string.IsNullOrEmpty(prod.Variant) ? prod.Name : $"{prod.Name} ({prod.Variant})") : "Item";
                            itemsTableRows += $"<tr><td style='padding: 6px; border: 1px solid #dee2e6;'>{pName}</td><td style='padding: 6px; border: 1px solid #dee2e6; text-align: center;'>{item.Quantity}</td><td style='padding: 6px; border: 1px solid #dee2e6; text-align: right;'>PKR {item.UnitPrice:F2}</td><td style='padding: 6px; border: 1px solid #dee2e6; text-align: right;'>PKR {item.TotalAmount:F2}</td></tr>";
                        }
                    }

                    string emailBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
                            <h2 style='color: #212529; border-bottom: 2px solid #212529; padding-bottom: 10px; margin-top: 0;'>Sales Invoice Receipt</h2>
                            <p>Dear {customer.FullName},</p>
                            <p>Thank you for your purchase! A copy of your sales invoice ({sale.InvoiceNo}) is provided below:</p>
                            <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                                <thead style='background-color: #212529; color: #fff;'>
                                    <tr>
                                        <th style='padding: 8px; border: 1px solid #dee2e6; text-align: left;'>Product</th>
                                        <th style='padding: 8px; border: 1px solid #dee2e6; text-align: center;'>Qty</th>
                                        <th style='padding: 8px; border: 1px solid #dee2e6; text-align: right;'>Unit Price</th>
                                        <th style='padding: 8px; border: 1px solid #dee2e6; text-align: right;'>Total</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {itemsTableRows}
                                </tbody>
                                <tfoot>
                                    <tr style='background-color: #f8f9fa; font-weight: bold;'>
                                        <td colspan='3' style='padding: 8px; border: 1px solid #dee2e6; text-align: right;'>Grand Total</td>
                                        <td style='padding: 8px; border: 1px solid #dee2e6; text-align: right; color: #212529;'>PKR {sale.TotalAmount:F2}</td>
                                    </tr>
                                </tfoot>
                            </table>
                            {(string.IsNullOrEmpty(sale.Notes) ? "" : $"<p><strong>Notes:</strong> {sale.Notes}</p>")}
                            <p>If you have any questions or require further assistance, feel free to reply to this email.</p>
                            <p>Best regards,<br/><strong>IMS Sales Team</strong></p>
                        </div>";

                    await _emailSender.SendEmailAsync(customer.Email, $"Your IMS Sales Invoice Receipt - {sale.InvoiceNo}", emailBody);
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "SMTP receipt dispatch failed for Sale Invoice {InvoiceNo}", sale.InvoiceNo);
            }

            return Json(new { success = true, message = "Sale invoice created and stock out processed successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromBody] Sale sale)
        {
            if (id != sale.Id)
            {
                return Json(new { success = false, message = "Sale ID mismatch." });
            }

            ModelState.Remove(nameof(sale.InvoiceNo));
            ModelState.Remove(nameof(sale.ProductId));
            ModelState.Remove(nameof(sale.Quantity));
            ModelState.Remove(nameof(sale.UnitPrice));

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            var existingSale = await _context.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existingSale == null)
            {
                return Json(new { success = false, message = "Sale record not found." });
            }

            if (sale.Items == null || !sale.Items.Any())
            {
                return Json(new { success = false, message = "At least one item is required for the sale invoice." });
            }

            // Validate all new items
            foreach (var item in sale.Items)
            {
                var prod = await _context.Products.FindAsync(item.ProductId);
                if (prod == null)
                {
                    return Json(new { success = false, message = $"Product with ID {item.ProductId} not found." });
                }
                if (item.UnitPrice < prod.Price)
                {
                    return Json(new { success = false, message = $"Unit price for '{prod.Name}' cannot be less than set price (PKR {prod.Price:F2})." });
                }
                item.TotalAmount = item.Quantity * item.UnitPrice;
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Revert stock of existing sale items
                    if (existingSale.Items != null && existingSale.Items.Any())
                    {
                        foreach (var oldItem in existingSale.Items)
                        {
                            var prod = await _context.Products.FindAsync(oldItem.ProductId);
                            if (prod != null) prod.StockQuantity += oldItem.Quantity;
                        }
                        _context.SaleItems.RemoveRange(existingSale.Items);
                    }
                    else if (existingSale.ProductId.HasValue)
                    {
                        var prod = await _context.Products.FindAsync(existingSale.ProductId.Value);
                        if (prod != null) prod.StockQuantity += existingSale.Quantity;
                    }

                    // 2. Check and deduct stock for new items
                    var newProductGroupedQty = sale.Items
                        .GroupBy(i => i.ProductId)
                        .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(x => x.Quantity) })
                        .ToList();

                    foreach (var group in newProductGroupedQty)
                    {
                        var prod = await _context.Products.FindAsync(group.ProductId);
                        if (prod!.StockQuantity < group.TotalQty)
                        {
                            throw new BusinessException($"Cannot update sale invoice. Insufficient stock for '{prod.Name}' (Available: {prod.StockQuantity}, Requested: {group.TotalQty}).");
                        }
                        prod.StockQuantity -= group.TotalQty;
                    }

                    // Update properties on existingSale
                    existingSale.CustomerId = sale.CustomerId;
                    existingSale.Notes = sale.Notes;
                    existingSale.PaymentMethod = sale.PaymentMethod;
                    existingSale.PaymentReference = sale.PaymentReference;
                    existingSale.BankName = sale.BankName;
                    existingSale.CheckDate = sale.CheckDate;

                    existingSale.TotalAmount = sale.Items.Sum(i => i.TotalAmount);
                    existingSale.Quantity = sale.Items.Sum(i => i.Quantity);
                    existingSale.ProductId = sale.Items.First().ProductId;
                    existingSale.UnitPrice = sale.Items.Count == 1 ? sale.Items.First().UnitPrice : 0;
                    existingSale.BatchNumber = sale.Items.First().BatchNumber;

                    foreach (var item in sale.Items)
                    {
                        item.SaleId = id;
                        _context.SaleItems.Add(item);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Sale invoice updated successfully!" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    if (ex is BusinessException) throw;
                    throw new BusinessException($"Failed to update sale: {ex.Message}");
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
            {
                return Json(new { success = false, message = "Sale record not found." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Return stock back for all items
                    if (sale.Items != null && sale.Items.Any())
                    {
                        foreach (var item in sale.Items)
                        {
                            var prod = await _context.Products.FindAsync(item.ProductId);
                            if (prod != null) prod.StockQuantity += item.Quantity;
                        }
                    }
                    else if (sale.ProductId.HasValue)
                    {
                        var prod = await _context.Products.FindAsync(sale.ProductId.Value);
                        if (prod != null) prod.StockQuantity += sale.Quantity;
                    }

                    _context.Sales.Remove(sale);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Sale invoice deleted and items restocked." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new BusinessException($"Failed to delete sale: {ex.Message}");
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.Product)
                .Include(s => s.Customer)
                .Include(s => s.Installments)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (sale == null)
            {
                return NotFound();
            }

            ViewData["CompanyProfile"] = await _context.CompanyProfiles.FirstOrDefaultAsync();
            return View(sale);
        }

        [HttpGet]
        public async Task<IActionResult> GetInstallments(int saleId)
        {
            var installments = await _context.SaleInstallments
                .Where(si => si.SaleId == saleId)
                .OrderBy(si => si.InstallmentNumber)
                .Select(si => new
                {
                    si.Id,
                    si.InstallmentNumber,
                    si.Amount,
                    si.PaidAmount,
                    DueDate = si.DueDate.ToString("yyyy-MM-dd"),
                    PaymentDate = si.PaymentDate.HasValue ? si.PaymentDate.Value.ToString("yyyy-MM-dd HH:mm") : "-",
                    si.Status,
                    PaymentMethod = si.PaymentMethod.HasValue ? (int)si.PaymentMethod.Value : (int?)null,
                    PaymentMethodName = si.PaymentMethod.HasValue ? si.PaymentMethod.Value.ToString() : null,
                    PaymentReference = si.PaymentReference ?? "",
                    BankName = si.BankName ?? "",
                    CheckDate = si.CheckDate.HasValue ? si.CheckDate.Value.ToString("yyyy-MM-dd") : ""
                })
                .ToListAsync();

            return Json(new { success = true, data = installments });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveInstallment([FromBody] ReceiveInstallmentModel model)
        {
            if (model == null || model.InstallmentId <= 0 || model.Amount <= 0)
            {
                return Json(new { success = false, message = "Invalid collection data." });
            }

            var installment = await _context.SaleInstallments
                .Include(si => si.Sale)
                .FirstOrDefaultAsync(si => si.Id == model.InstallmentId);

            if (installment == null)
            {
                return Json(new { success = false, message = "Installment record not found." });
            }

            if (installment.Status == "Paid")
            {
                return Json(new { success = false, message = "This installment is already fully paid." });
            }

            decimal remainingToPay = installment.Amount - installment.PaidAmount;
            if (model.Amount > remainingToPay)
            {
                return Json(new { success = false, message = $"Collection amount exceeds the remaining installment balance of PKR {remainingToPay:F2}." });
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

            return Json(new { success = true, message = "Payment collection recorded successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendReminderEmail([FromBody] ReminderEmailModel model)
        {
            if (model == null || model.InstallmentId <= 0)
            {
                return Json(new { success = false, message = "Invalid installment selection." });
            }

            var installment = await _context.SaleInstallments
                .Include(si => si.Sale!)
                    .ThenInclude(s => s.Customer)
                .Include(si => si.Sale!)
                    .ThenInclude(s => s.Product)
                .Include(si => si.Sale!)
                    .ThenInclude(s => s.Items)
                        .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(si => si.Id == model.InstallmentId);

            if (installment == null)
            {
                return Json(new { success = false, message = "Installment not found." });
            }

            var customer = installment.Sale?.Customer;
            if (customer == null || string.IsNullOrEmpty(customer.Email))
            {
                return Json(new { success = false, message = "Customer email details not found." });
            }

            string productName = installment.Sale?.Product?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(productName) && installment.Sale?.Items != null && installment.Sale.Items.Any())
            {
                productName = string.Join(", ", installment.Sale.Items.Select(i => i.Product != null ? (!string.IsNullOrWhiteSpace(i.Product.Variant) ? $"{i.Product.Name} ({i.Product.Variant})" : i.Product.Name) : "Product"));
            }
            if (string.IsNullOrWhiteSpace(productName))
            {
                productName = "Purchased Items";
            }

            string subject = $"Reminder: Lease Installment Payment due for invoice {installment.Sale!.InvoiceNo}";

            string htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
                    <h2 style='color: #333; border-bottom: 2px solid #f0ad4e; padding-bottom: 10px;'>Lease Installment Payment Reminder</h2>
                    <p>Dear <strong>{customer.FullName}</strong>,</p>
                    <p>This is a friendly reminder that installment <strong>#{installment.InstallmentNumber}</strong> for your purchase of <strong>{productName}</strong> is due.</p>
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                        <tr style='background-color: #f8f9fa;'>
                            <th style='padding: 10px; border: 1px solid #ddd; text-align: left;'>Invoice Number</th>
                            <td style='padding: 10px; border: 1px solid #ddd;'>{installment.Sale.InvoiceNo}</td>
                        </tr>
                        <tr>
                            <th style='padding: 10px; border: 1px solid #ddd; text-align: left;'>Installment Number</th>
                            <td style='padding: 10px; border: 1px solid #ddd;'>#{installment.InstallmentNumber}</td>
                        </tr>
                        <tr style='background-color: #f8f9fa;'>
                            <th style='padding: 10px; border: 1px solid #ddd; text-align: left;'>Amount Due</th>
                            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold; color: #d9534f;'>PKR {installment.Amount:F2}</td>
                        </tr>
                        <tr>
                            <th style='padding: 10px; border: 1px solid #ddd; text-align: left;'>Due Date</th>
                            <td style='padding: 10px; border: 1px solid #ddd;'>{installment.DueDate.ToString("yyyy-MM-dd")}</td>
                        </tr>
                    </table>
                    <p>Please submit the installment payment at your earliest convenience. If you have already made the payment, please ignore this email.</p>
                    <p>Best regards,<br/><strong>Inventory Management System team</strong></p>
                </div>";

            try
            {
                await _emailSender.SendEmailAsync(customer.Email, subject, htmlMessage);
                return Json(new { success = true, message = "Reminder email sent successfully to " + customer.Email });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "Failed to send email: " + ex.Message });
            }
        }
    }
}
