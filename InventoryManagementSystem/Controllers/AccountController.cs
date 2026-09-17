using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Models.ViewModels;
using InventoryManagementSystem.Services;
using System;

namespace InventoryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard();
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid credentials format." });
            }

            var existingUser = await _userManager.FindByNameAsync(model.Username) ?? await _userManager.FindByEmailAsync(model.Username);
            if (existingUser != null && await _userManager.IsLockedOutAsync(existingUser))
            {
                return Json(new { success = false, message = "Your user account has been restricted. Please contact system administrator." });
            }

            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                string redirectUrl = returnUrl ?? await GetDashboardUrlForUser(user);
                return Json(new { success = true, redirectUrl });
            }
            if (result.IsNotAllowed)
            {
                return Json(new { success = false, message = "You must confirm your email before you can log in." });
            }

            return Json(new { success = false, message = "Invalid username or password." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid profile data." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            user.FullName = model.FullName;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                string errors = string.Join(" ", updateResult.Errors.Select(e => e.Description));
                return Json(new { success = false, message = errors });
            }

            if (!string.IsNullOrEmpty(model.CurrentPassword) && !string.IsNullOrEmpty(model.NewPassword))
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    string errors = string.Join(" ", changePasswordResult.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = "Profile updated, but password change failed: " + errors });
                }
            }

            return Json(new { success = true, message = "Profile updated successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                ViewBag.Success = false;
                return View();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.Success = false;
                return View();
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            ViewBag.Success = result.Succeeded;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> VerifyUsername(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            return Json(user == null);
        }

        private IActionResult RedirectToDashboard()
        {
            if (User.IsInRole("Admin") || User.HasClaim("Permission", "Dashboard"))
            {
                return RedirectToAction("Index", "Admin");
            }
            if (User.HasClaim("Permission", "Sales")) return RedirectToAction("Index", "Sales");
            if (User.HasClaim("Permission", "Purchases")) return RedirectToAction("Index", "Purchases");
            if (User.HasClaim("Permission", "Reports")) return RedirectToAction("Index", "Reports");
            if (User.HasClaim("Permission", "Inventory")) return RedirectToAction("Index", "Products");
            if (User.HasClaim("Permission", "Staff")) return RedirectToAction("Index", "MasterSettings", new { tab = "employees" });
            if (User.HasClaim("Permission", "Settings")) return RedirectToAction("Index", "MasterSettings", new { tab = "company" });

            return RedirectToAction("Index", "Home");
        }

        private async Task<string> GetDashboardUrlForUser(ApplicationUser? user)
        {
            if (user == null) return Url.Action("Index", "Home") ?? "/";

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Url.Action("Index", "Admin") ?? "/";
            }

            var claims = await _userManager.GetClaimsAsync(user);
            var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToHashSet();

            if (permissions.Contains("Dashboard")) return Url.Action("Index", "Admin") ?? "/";
            if (permissions.Contains("Sales")) return Url.Action("Index", "Sales") ?? "/";
            if (permissions.Contains("Purchases")) return Url.Action("Index", "Purchases") ?? "/";
            if (permissions.Contains("Reports")) return Url.Action("Index", "Reports") ?? "/";
            if (permissions.Contains("Inventory")) return Url.Action("Index", "Products") ?? "/";
            if (permissions.Contains("Staff")) return Url.Action("Index", "MasterSettings", new { tab = "employees" }) ?? "/";
            if (permissions.Contains("Settings")) return Url.Action("Index", "MasterSettings", new { tab = "company" }) ?? "/";

            return Url.Action("Index", "Home") ?? "/";
        }
    }
}
