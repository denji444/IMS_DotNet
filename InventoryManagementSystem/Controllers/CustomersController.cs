using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Exceptions;
using InventoryManagementSystem.Services;
using System;

namespace InventoryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CustomersController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public CustomersController(
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

        // DataTables AJAX Endpoint
        [HttpGet]
        public async Task<IActionResult> GetCustomersData()
        {
            var customerRoleId = await _context.Roles
                .Where(r => r.Name == "Customer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var customers = await _context.Users
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == customerRoleId))
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.Cnic,
                    CreatedAt = u.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                })
                .ToListAsync();

            return Json(new { data = customers });
        }

        // Select2 Endpoint for AJAX autocomplete in Sales creation
        [HttpGet]
        public async Task<IActionResult> GetCustomersJson(string? q)
        {
            var customerRoleId = await _context.Roles
                .Where(r => r.Name == "Customer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var query = _context.Users
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == customerRoleId));

            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(u => u.UserName!.Contains(q) || u.FullName.Contains(q));
            }

            var data = await query.Select(u => new { id = u.Id, text = $"{u.FullName} ({u.UserName})" }).ToListAsync();
            return Json(data);
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> VerifyUsername(string username, string? id)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user != null && user.Id != id)
            {
                return Json($"Username '{username}' is already taken.");
            }
            return Json(true);
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomer(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var nameParts = (user.FullName ?? "").Split(' ', 2);
            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            return Json(new
            {
                user.Id,
                FirstName = firstName,
                LastName = lastName,
                user.Email,
                user.PhoneNumber,
                user.Cnic
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CustomerInputModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            // Verify unique email
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                return Json(new { success = false, message = $"Email '{model.Email}' is already registered." });
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = $"{model.FirstName} {model.LastName}".Trim(),
                PhoneNumber = model.Phone,
                Cnic = model.Cnic,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true // auto-confirm email as customer dashboard is no longer needed
            };

            var randomPassword = Guid.NewGuid().ToString() + "aA1!";
            var result = await _userManager.CreateAsync(user, randomPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");

                // Send simple welcome email
                try
                {
                    string emailBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
                            <h2 style='color: #212529; text-align: center; border-bottom: 2px solid #212529; padding-bottom: 10px;'>Welcome to IMS Portal</h2>
                            <p>Hello {user.FullName},</p>
                            <p>Your customer profile has been registered in our Inventory Management System under this email address.</p>
                            <p>Best regards,<br/><strong>IMS Support Team</strong></p>
                        </div>";

                    await _emailSender.SendEmailAsync(user.Email, "Profile Registered - IMS", emailBody);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SMTP delivery failed: {ex.Message}");
                }

                return Json(new { success = true, message = "Customer created successfully!" });
            }

            string errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = errors });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [FromBody] CustomerInputModel model)
        {
            if (id != model.Id)
            {
                return Json(new { success = false, message = "Customer ID mismatch." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Customer not found." });
            }

            // Verify email uniqueness if changed
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailUser = await _userManager.FindByEmailAsync(model.Email);
                if (emailUser != null && emailUser.Id != id)
                {
                    return Json(new { success = false, message = $"Email '{model.Email}' is already registered to another user." });
                }
            }

            user.FullName = $"{model.FirstName} {model.LastName}".Trim();
            user.Email = model.Email;
            user.UserName = model.Email; // Keep username synced with email
            user.PhoneNumber = model.Phone;
            user.Cnic = model.Cnic;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Customer updated successfully!" });
            }

            string errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = errors });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Customer not found." });
            }

            // Condition check: Customer can only be deleted if no sales / stockouts are associated
            bool hasSales = await _context.Sales.AnyAsync(s => s.CustomerId == id);
            if (hasSales)
            {
                throw new BusinessException("Cannot delete customer because sales / stock-out transactions are linked to this customer.");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Customer deleted successfully!" });
            }

            string errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = errors });
        }
    }

    // Input DTO model for AJAX validation
    public class CustomerInputModel
    {
        public string? Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Cnic { get; set; }
    }
}
