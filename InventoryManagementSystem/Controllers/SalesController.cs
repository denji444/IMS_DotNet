using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class SalesController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public SalesController(
            InventoryDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            return View();
        }

        // DataTables Endpoint
        [HttpGet]
        public async Task<IActionResult> GetSalesData()
        {
            var sales = await _context.Sales
                .Include(s => s.Product)
                .Include(s => s.Customer)
                .Select(s => new
                {
                    s.Id,
                    s.InvoiceNo,
                    ProductName = s.Product != null ? s.Product.Name : "N/A",
                    CustomerName = s.Customer != null ? s.Customer.FullName : "N/A",
                    s.Quantity,
                    s.UnitPrice,
                    s.TotalAmount,
                    SaleDate = s.SaleDate.ToString("yyyy-MM-dd HH:mm"),
                    s.Notes,
                    PaymentMode = (int)s.PaymentMode
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
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (sale == null)
            {
                return NotFound();
            }

            return Json(new
            {
                sale.Id,
                sale.InvoiceNo,
                sale.ProductId,
                ProductName = sale.Product != null ? (string.IsNullOrEmpty(sale.Product.Variant) ? $"{sale.Product.Name} ({sale.Product.Sku})" : $"{sale.Product.Name} ({sale.Product.Variant}) [{sale.Product.Sku}]") : null,
                sale.CustomerId,
                CustomerName = sale.Customer?.FullName,
                sale.Quantity,
                sale.UnitPrice,
                sale.TotalAmount,
                sale.Notes,
                PaymentMode = (int)sale.PaymentMode,
                sale.DownPayment,
                sale.InstallmentsCount,
                sale.InstallmentFrequency
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
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = $"Invalid data submitted: {errors}" });
            }

            var product = await _context.Products.FindAsync(sale.ProductId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            // Condition: Check if UnitPrice is less than product's set price
            if (sale.UnitPrice < product.Price)
            {
                return Json(new { success = false, message = $"Unit price cannot be less than the product's set price (PKR {product.Price:F2})." });
            }

            // Condition: Check if stock is sufficient
            if (product.StockQuantity < sale.Quantity)
            {
                throw new BusinessException($"Insufficient stock. Requested stock out: {sale.Quantity}, Available in stock: {product.StockQuantity}");
            }

            // Generate InvoiceNo: INV-yyyyMMdd-XXXX
            var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _context.Sales.CountAsync(s => s.InvoiceNo.StartsWith($"INV-{dateStr}")) + 1;
            sale.InvoiceNo = $"INV-{dateStr}-{count:D4}";

            sale.TotalAmount = sale.Quantity * sale.UnitPrice;
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

                    // Decrement stock
                    product.StockQuantity -= sale.Quantity;

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
                    string emailBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
                            <h2 style='color: #212529; border-bottom: 2px solid #212529; padding-bottom: 10px; margin-top: 0;'>Sales Invoice Receipt</h2>
                            <p>Dear {customer.FullName},</p>
                            <p>Thank you for your purchase! A copy of your sales invoice has been generated below:</p>
                            <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                                <tr style='background-color: #f8f9fa;'>
                                    <td style='padding: 8px; border: 1px solid #dee2e6; font-weight: bold;'>Invoice Number</td>
                                    <td style='padding: 8px; border: 1px solid #dee2e6;'>{sale.InvoiceNo}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; border: 1px solid #dee2e6; font-weight: bold;'>Product</td>
                                    <td style='padding: 8px; border: 1px solid #dee2e6;'>{product.Name} (SKU: {product.Sku})</td>
                                </tr>
                                <tr style='background-color: #f8f9fa;'>
                                    <td style='padding: 8px; border: 1px solid #dee2e6; font-weight: bold;'>Quantity</td>
                                    <td style='padding: 8px; border: 1px solid #dee2e6;'>{sale.Quantity}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; border: 1px solid #dee2e6; font-weight: bold;'>Unit Price</td>
                                    <td style='padding: 8px; border: 1px solid #dee2e6;'>PKR {sale.UnitPrice:F2}</td>
                                </tr>
                                <tr style='background-color: #f8f9fa; font-weight: bold;'>
                                    <td style='padding: 8px; border: 1px solid #dee2e6; color: #212529;'>Total Paid</td>
                                    <td style='padding: 8px; border: 1px solid #dee2e6; color: #212529;'>PKR {sale.TotalAmount:F2}</td>
                                </tr>
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
                Console.WriteLine($"SMTP receipt dispatch failed: {emailEx.Message}");
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
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            var existingSale = await _context.Sales.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (existingSale == null)
            {
                return Json(new { success = false, message = "Sale record not found." });
            }

            var product = await _context.Products.FindAsync(sale.ProductId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            // Condition: Check if UnitPrice is less than product's set price
            if (sale.UnitPrice < product.Price)
            {
                return Json(new { success = false, message = $"Unit price cannot be less than the product's set price (PKR {product.Price:F2})." });
            }

            sale.TotalAmount = sale.Quantity * sale.UnitPrice;
            sale.InvoiceNo = existingSale.InvoiceNo; // Keep original invoice no
            sale.SaleDate = existingSale.SaleDate; // Keep original date

            // Keep original lease configuration when editing
            sale.PaymentMode = existingSale.PaymentMode;
            sale.DownPayment = existingSale.DownPayment;
            sale.InstallmentsCount = existingSale.InstallmentsCount;
            sale.InstallmentFrequency = existingSale.InstallmentFrequency;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Adjust Stock: add back old qty, subtract new qty (net change is new - old)
                    int stockDiff = sale.Quantity - existingSale.Quantity;
                    if (product.StockQuantity - stockDiff < 0)
                    {
                        throw new BusinessException($"Cannot update sale. Adjusting stock would leave product stock negative (Current Stock: {product.StockQuantity}, Requested additional stock out: {stockDiff}).");
                    }

                    product.StockQuantity -= stockDiff;

                    _context.Entry(sale).State = EntityState.Modified;
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
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null)
            {
                return Json(new { success = false, message = "Sale record not found." });
            }

            var product = await _context.Products.FindAsync(sale.ProductId);
            if (product == null)
            {
                return Json(new { success = false, message = "Associated product not found." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Return stock back
                    product.StockQuantity += sale.Quantity;

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
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (sale == null)
            {
                return NotFound();
            }

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
                    si.Status
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
            if (installment.PaidAmount == installment.Amount)
            {
                installment.Status = "Paid";
                installment.PaymentDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Payment collection recorded successfully!" });
        }

        public class ReceiveInstallmentModel
        {
            public int InstallmentId { get; set; }
            public decimal Amount { get; set; }
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

            string subject = $"Reminder: Lease Installment Payment due for invoice {installment.Sale!.InvoiceNo}";

            string htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
                    <h2 style='color: #333; border-bottom: 2px solid #f0ad4e; padding-bottom: 10px;'>Lease Installment Payment Reminder</h2>
                    <p>Dear <strong>{customer.FullName}</strong>,</p>
                    <p>This is a friendly reminder that installment <strong>#{installment.InstallmentNumber}</strong> for your purchase of <strong>{installment.Sale.Product?.Name}</strong> is due.</p>
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

        public class ReminderEmailModel
        {
            public int InstallmentId { get; set; }
        }
    }
}
