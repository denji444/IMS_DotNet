using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Exceptions;
using InventoryManagementSystem.Services;
using System;

using Microsoft.Extensions.Logging;

namespace InventoryManagementSystem.Controllers
{
    [Authorize(Policy = "Purchases")]
    public class SuppliersController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<SuppliersController> _logger;

        public SuppliersController(InventoryDbContext context, IEmailSender emailSender, ILogger<SuppliersController> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Purchases", new { tab = "suppliers" });
        }

        // Endpoint for DataTables AJAX
        [HttpGet]
        public async Task<IActionResult> GetSuppliersData()
        {
            var suppliers = await _context.Suppliers.ToListAsync();

            var multiItemProducts = await _context.PurchaseItems
                .Where(pi => pi.Product != null && pi.Purchase != null)
                .Select(pi => new
                {
                    SupplierId = pi.Purchase!.SupplierId,
                    Name = pi.Product!.Name,
                    Variant = pi.Product.Variant,
                    Sku = pi.Product.Sku
                })
                .Distinct()
                .ToListAsync();

            var legacyProducts = await _context.Purchases
                .Where(pu => pu.Product != null && !pu.Items.Any())
                .Select(pu => new
                {
                    pu.SupplierId,
                    Name = pu.Product!.Name,
                    Variant = pu.Product.Variant,
                    Sku = pu.Product.Sku
                })
                .Distinct()
                .ToListAsync();

            var allPurchased = multiItemProducts.Concat(legacyProducts)
                .GroupBy(x => x.SupplierId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new { name = x.Name, variant = x.Variant, sku = x.Sku }).Distinct().ToList()
                );

            var data = suppliers.Select(s => new
            {
                s.Id,
                s.Name,
                s.Email,
                s.Phone,
                s.Address,
                s.Cnic,
                s.IsEmailVerified,
                Products = allPurchased.TryGetValue(s.Id, out var prodList) ? (object)prodList : Array.Empty<object>()
            }).ToList();

            return Json(new { data = data });
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> VerifyName(string name, int id)
        {
            var exists = await _context.Suppliers.AnyAsync(s => s.Name == name && s.Id != id);
            if (exists)
            {
                return Json($"Supplier name '{name}' is already in use.");
            }
            return Json(true);
        }

        // Endpoint for Select2 AJAX binding
        [HttpGet]
        public async Task<IActionResult> GetSuppliersJson(string? q)
        {
            var query = _context.Suppliers.AsQueryable();
            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(s => s.Name.Contains(q) || s.ContactName.Contains(q));
            }
            var data = await query.Select(s => new { id = s.Id, text = s.Name }).ToListAsync();
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return Json(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] Supplier supplier)
        {
            supplier.ContactName = supplier.Name;
            ModelState.Remove(nameof(supplier.ContactName));

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            var nameExists = await _context.Suppliers.AnyAsync(s => s.Name == supplier.Name && s.Id != supplier.Id);
            if (nameExists)
            {
                return Json(new { success = false, message = "Supplier name is already in use." });
            }

            supplier.IsEmailVerified = false;
            supplier.EmailVerificationToken = Guid.NewGuid().ToString();

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            // Send Verification Email to Supplier
            var callbackUrl = Url.Action("VerifyEmail", "Suppliers", new { id = supplier.Id, token = supplier.EmailVerificationToken }, protocol: HttpContext.Request.Scheme);

            string emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
                    <h2 style='color: #212529; text-align: center; border-bottom: 2px solid #212529; padding-bottom: 10px;'>Confirm Your Email Address</h2>
                    <p>Hello {supplier.ContactName},</p>
                    <p>You have been registered as a supplier for the Inventory Management System under <strong>{supplier.Name}</strong>.</p>
                    <p>Please click the button below to verify your email address. This ensures you can receive automated purchase orders and restock notices from us:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{callbackUrl}' style='background-color: #212529; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Verify Email</a>
                    </div>
                    <p style='color: #666; font-size: 12px;'>Or copy and paste this URL into your browser: <br/>{callbackUrl}</p>
                </div>";

            try
            {
                await _emailSender.SendEmailAsync(supplier.Email, "Confirm Supplier Email - IMS", emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to supplier {Email}", supplier.Email);
            }

            return Json(new { success = true, message = "Supplier created successfully! A verification email has been sent." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromBody] Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return Json(new { success = false, message = "Supplier ID mismatch." });
            }

            supplier.ContactName = supplier.Name;
            ModelState.Remove(nameof(supplier.ContactName));

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            var nameExists = await _context.Suppliers.AnyAsync(s => s.Name == supplier.Name && s.Id != supplier.Id);
            if (nameExists)
            {
                return Json(new { success = false, message = "Supplier name is already in use." });
            }

            var existingSupplier = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (existingSupplier == null)
            {
                return Json(new { success = false, message = "Supplier not found." });
            }

            bool emailChanged = !string.Equals(existingSupplier.Email, supplier.Email, StringComparison.OrdinalIgnoreCase);
            if (emailChanged)
            {
                supplier.IsEmailVerified = false;
                supplier.EmailVerificationToken = Guid.NewGuid().ToString();
            }
            else
            {
                supplier.IsEmailVerified = existingSupplier.IsEmailVerified;
                supplier.EmailVerificationToken = existingSupplier.EmailVerificationToken;
            }

            try
            {
                _context.Entry(supplier).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                if (emailChanged)
                {
                    // Send Verification Email to Supplier
                    var callbackUrl = Url.Action("VerifyEmail", "Suppliers", new { id = supplier.Id, token = supplier.EmailVerificationToken }, protocol: HttpContext.Request.Scheme);

                    string emailBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
                            <h2 style='color: #212529; text-align: center; border-bottom: 2px solid #212529; padding-bottom: 10px;'>Verify Your New Email Address</h2>
                            <p>Hello {supplier.ContactName},</p>
                            <p>Your email address for <strong>{supplier.Name}</strong> has been updated in the Inventory Management System.</p>
                            <p>Please click the button below to verify your new email address to ensure you receive automated purchase orders:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{callbackUrl}' style='background-color: #212529; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Verify Email</a>
                            </div>
                            <p style='color: #666; font-size: 12px;'>Or copy and paste this URL into your browser: <br/>{callbackUrl}</p>
                        </div>";

                    try
                    {
                        await _emailSender.SendEmailAsync(supplier.Email, "Confirm Updated Supplier Email - IMS", emailBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send updated email verification to supplier {Email}", supplier.Email);
                    }
                }

                return Json(new { success = true, message = emailChanged ? "Supplier updated successfully! A verification email has been sent to the new address." : "Supplier updated successfully!" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SupplierExists(supplier.Id))
                {
                    return Json(new { success = false, message = "Supplier not found." });
                }
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return Json(new { success = false, message = "Supplier not found." });
            }

            // Condition: Cannot delete supplier if they have purchase records
            bool hasPurchases = await _context.Purchases.AnyAsync(p => p.SupplierId == id);
            if (hasPurchases)
            {
                // Throws custom BusinessException which is caught by AjaxExceptionFilter
                throw new BusinessException("Cannot delete supplier because they have associated purchase transactions.");
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Supplier deleted successfully!" });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail(int id, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                ViewBag.Success = false;
                return View();
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null || supplier.EmailVerificationToken != token)
            {
                ViewBag.Success = false;
                return View();
            }

            supplier.IsEmailVerified = true;
            supplier.EmailVerificationToken = null; // Clear the token

            _context.Entry(supplier).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            ViewBag.Success = true;
            ViewBag.Email = supplier.Email;
            return View();
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
    }
}
