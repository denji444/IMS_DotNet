using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Models.ViewModels;
using InventoryManagementSystem.Models.Configuration;
using InventoryManagementSystem.Exceptions;

using Microsoft.AspNetCore.Hosting;

namespace InventoryManagementSystem.Controllers
{
    [Authorize]
    public class MasterSettingsController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOptions<SmtpSettings> _smtpFallbackOptions;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public MasterSettingsController(
            InventoryDbContext context,
            UserManager<ApplicationUser> userManager,
            IOptions<SmtpSettings> smtpFallbackOptions,
            IDataProtectionProvider dataProtectionProvider,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _smtpFallbackOptions = smtpFallbackOptions;
            _protector = dataProtectionProvider.CreateProtector("InventoryManagementSystem.SmtpProtector");
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string? tab = null)
        {
            var requestedTab = tab?.ToLower() ?? "employees";
            if (requestedTab == "categories")
            {
                return RedirectToAction("Index", "Products", new { tab = "categories" });
            }

            string section;
            if (requestedTab == "company" || requestedTab == "smtp")
            {
                section = "Settings";
            }
            else
            {
                section = "Staff";
                if (requestedTab != "employees" && requestedTab != "departments" && requestedTab != "attendance" && requestedTab != "leaves")
                {
                    requestedTab = "employees";
                }
            }

            // Tab-level permission authorization guard for non-admins
            if (!User.IsInRole("Admin"))
            {
                bool isAllowed = User.HasClaim("Permission", section);
                if (!isAllowed)
                {
                    if (User.HasClaim("Permission", "Staff"))
                        return RedirectToAction(nameof(Index), new { tab = "employees" });
                    if (User.HasClaim("Permission", "Settings"))
                        return RedirectToAction(nameof(Index), new { tab = "company" });

                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            // Seed standard inventory departments if none exist
            if (!await _context.Departments.AnyAsync())
            {
                _context.Departments.AddRange(
                    new Department { Name = "Procurement / Purchasing", Description = "Manages supplier orders and stock intake." },
                    new Department { Name = "Warehouse / Inventory Control", Description = "Manages storage, stock counts, and transfers." },
                    new Department { Name = "Sales & Stock Out", Description = "Handles customer invoices and dispatch." },
                    new Department { Name = "Logistics & Dispatch", Description = "Manages delivery personnel and drivers." },
                    new Department { Name = "Quality Control", Description = "Inspects incoming shipments." }
                );
                await _context.SaveChangesAsync();
            }

            ViewBag.CurrentSection = section;
            ViewBag.ActiveTab = requestedTab;
            ViewBag.DepartmentList = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
            return View();
        }

        #region Departments AJAX Endpoints

        [HttpGet]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> GetDepartmentsData()
        {
            var data = await _context.Departments
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Description,
                    EmployeeCount = _context.Employees.Count(e => e.DepartmentId == d.Id)
                })
                .ToListAsync();

            return Json(new { data });
        }

        [HttpGet]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> GetDepartment(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();
            return Json(dept);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> SaveDepartment([FromBody] Department model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            var nameExists = await _context.Departments.AnyAsync(d => d.Name == model.Name && d.Id != model.Id);
            if (nameExists)
            {
                return Json(new { success = false, message = $"Department '{model.Name}' already exists." });
            }

            if (model.Id == 0)
            {
                _context.Departments.Add(model);
            }
            else
            {
                _context.Entry(model).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Department saved successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null)
            {
                return Json(new { success = false, message = "Department not found." });
            }

            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Department deleted successfully! Associated employees have been unassigned." });
        }

        #endregion

        #region Employees AJAX Endpoints

        [HttpGet]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> GetEmployeesData()
        {
            var data = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Department)
                .Select(e => new
                {
                    e.Id,
                    UserId = e.User != null ? e.User.Id : "",
                    FullName = e.User != null ? e.User.FullName : "",
                    Email = e.User != null ? e.User.Email : "",
                    Phone = e.User != null ? e.User.PhoneNumber : "",
                    Designation = e.Designation,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.Name : "Unassigned",
                    HireDate = e.HireDate.ToString("yyyy-MM-dd"),
                    Salary = e.Salary,
                    Status = e.Status
                })
                .ToListAsync();

            return Json(new { data });
        }

        [HttpGet]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            var emp = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (emp == null) return NotFound();

            return Json(new
            {
                emp.Id,
                FullName = emp.User?.FullName ?? "",
                Email = emp.User?.Email ?? "",
                Phone = emp.User?.PhoneNumber ?? "",
                emp.Designation,
                emp.DepartmentId,
                HireDate = emp.HireDate.ToString("yyyy-MM-dd"),
                emp.Salary,
                emp.Status
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> SaveEmployee([FromBody] EmployeeInputModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            if (model.Id == 0) // Create Mode
            {
                // Verify unique email
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = $"Email '{model.Email}' is already registered in the system." });
                }

                // 1. Create ApplicationUser
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.Phone,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var randomPassword = Guid.NewGuid().ToString() + "aA1!";
                var createResult = await _userManager.CreateAsync(user, randomPassword);
                if (!createResult.Succeeded)
                {
                    string errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = errors });
                }

                // Assign Employee Role
                await _userManager.AddToRoleAsync(user, "Employee");

                // 2. Create Employee domain record
                var employee = new Employee
                {
                    UserId = user.Id,
                    DepartmentId = model.DepartmentId,
                    Designation = model.Designation,
                    HireDate = model.HireDate,
                    Salary = model.Salary,
                    Status = model.Status
                };

                _context.Employees.Add(employee);
            }
            else // Update Mode
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == model.Id);

                if (employee == null)
                {
                    return Json(new { success = false, message = "Employee not found." });
                }

                if (employee.User != null)
                {
                    // Check email uniqueness if email changed
                    if (!string.Equals(employee.User.Email, model.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        var emailUser = await _userManager.FindByEmailAsync(model.Email);
                        if (emailUser != null && emailUser.Id != employee.User.Id)
                        {
                            return Json(new { success = false, message = $"Email '{model.Email}' is already registered to another user." });
                        }
                    }

                    employee.User.FullName = model.FullName;
                    employee.User.Email = model.Email;
                    employee.User.UserName = model.Email;
                    employee.User.PhoneNumber = model.Phone;

                    var updateResult = await _userManager.UpdateAsync(employee.User);
                    if (!updateResult.Succeeded)
                    {
                        string errors = string.Join(" ", updateResult.Errors.Select(e => e.Description));
                        return Json(new { success = false, message = errors });
                    }
                }

                employee.DepartmentId = model.DepartmentId;
                employee.Designation = model.Designation;
                employee.HireDate = model.HireDate;
                employee.Salary = model.Salary;
                employee.Status = model.Status;

                _context.Entry(employee).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Employee saved successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            var user = employee.User;

            // Remove Employee record (cascade deletes attendance and leaves)
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            // Clean up corresponding system user
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            return Json(new { success = true, message = "Employee and their user account deleted successfully." });
        }

        [HttpGet]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> GetEmployeesJson(string? q)
        {
            var query = _context.Employees
                .Include(e => e.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(e =>
                    (e.User != null && (e.User.FullName.Contains(q) || (e.User.Email != null && e.User.Email.Contains(q)))) ||
                    e.Designation.Contains(q));
            }

            var data = await query
                .Select(e => new
                {
                    id = e.Id,
                    text = e.User != null ? $"{e.User.FullName} ({e.Designation})" : $"Employee #{e.Id}"
                })
                .ToListAsync();

            return Json(data);
        }

        [HttpGet]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> GetEmployeeUserAccount(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return Json(new { success = false, message = "Employee record not found." });
            }

            if (employee.User == null)
            {
                return Json(new { success = false, message = "No user account linked to this employee." });
            }

            bool isRestricted = await _userManager.IsLockedOutAsync(employee.User);
            var claims = await _userManager.GetClaimsAsync(employee.User);
            var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();

            return Json(new
            {
                success = true,
                employeeId = employee.Id,
                fullName = employee.User.FullName,
                email = employee.User.Email ?? "",
                userName = employee.User.UserName ?? "",
                designation = employee.Designation,
                departmentName = employee.Department?.Name ?? "Unassigned",
                defaultPassword = "Default@123",
                isRestricted = isRestricted,
                permissions = permissions,
                lockoutEnd = employee.User.LockoutEnd.HasValue && employee.User.LockoutEnd.Value > DateTimeOffset.UtcNow
                    ? employee.User.LockoutEnd.Value.ToString("yyyy-MM-dd HH:mm")
                    : null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> SaveEmployeePermissions([FromBody] EmployeePermissionsModel model)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == model.EmployeeId);

            if (employee == null || employee.User == null)
            {
                return Json(new { success = false, message = "Employee or user account not found." });
            }

            var existingClaims = await _userManager.GetClaimsAsync(employee.User);
            var permissionClaims = existingClaims.Where(c => c.Type == "Permission").ToList();

            if (permissionClaims.Any())
            {
                var removeResult = await _userManager.RemoveClaimsAsync(employee.User, permissionClaims);
                if (!removeResult.Succeeded)
                {
                    string errors = string.Join(" ", removeResult.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = "Failed to update permissions: " + errors });
                }
            }

            if (model.Permissions != null && model.Permissions.Any())
            {
                var newClaims = model.Permissions
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct()
                    .Select(p => new Claim("Permission", p))
                    .ToList();

                var addResult = await _userManager.AddClaimsAsync(employee.User, newClaims);
                if (!addResult.Succeeded)
                {
                    string errors = string.Join(" ", addResult.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = "Failed to assign permissions: " + errors });
                }
            }

            return Json(new { success = true, message = "Employee menu permissions updated successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> ResetEmployeePassword([FromBody] ResetPasswordInputModel model)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == model.EmployeeId);

            if (employee == null || employee.User == null)
            {
                return Json(new { success = false, message = "Employee or linked user account not found." });
            }

            var user = employee.User;
            string defaultPassword = "Default@123";

            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded && await _userManager.HasPasswordAsync(user))
            {
                string errors = string.Join(" ", removeResult.Errors.Select(e => e.Description));
                return Json(new { success = false, message = "Failed to clear existing password: " + errors });
            }

            var addResult = await _userManager.AddPasswordAsync(user, defaultPassword);
            if (!addResult.Succeeded)
            {
                string errors = string.Join(" ", addResult.Errors.Select(e => e.Description));
                return Json(new { success = false, message = "Failed to set default password: " + errors });
            }

            return Json(new
            {
                success = true,
                message = $"Password for {user.FullName} has been reset to default.",
                defaultPassword = defaultPassword
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> ToggleEmployeeAccountStatus([FromBody] AccountStatusToggleModel model)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == model.EmployeeId);

            if (employee == null || employee.User == null)
            {
                return Json(new { success = false, message = "Employee or linked user account not found." });
            }

            var user = employee.User;

            if (model.IsRestricted)
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                var lockoutResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                if (!lockoutResult.Succeeded)
                {
                    string errors = string.Join(" ", lockoutResult.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = "Failed to restrict account: " + errors });
                }
            }
            else
            {
                var lockoutResult = await _userManager.SetLockoutEndDateAsync(user, null);
                if (!lockoutResult.Succeeded)
                {
                    string errors = string.Join(" ", lockoutResult.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = "Failed to activate account: " + errors });
                }
            }

            string statusMsg = model.IsRestricted
                ? $"Account for {user.FullName} has been restricted."
                : $"Account for {user.FullName} has been activated.";

            return Json(new { success = true, isRestricted = model.IsRestricted, message = statusMsg });
        }

        #endregion

        #region Attendance AJAX Endpoints

        [HttpGet]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> GetAttendanceData()
        {
            var rawData = await _context.EmployeeAttendances
                .Include(ea => ea.Employee)
                .ThenInclude(e => e!.User)
                .OrderByDescending(ea => ea.Date)
                .ToListAsync();

            var data = rawData.Select(ea => new
            {
                ea.Id,
                ea.EmployeeId,
                EmployeeName = ea.Employee != null && ea.Employee.User != null ? ea.Employee.User.FullName : "Unknown",
                Date = ea.Date.ToString("yyyy-MM-dd"),
                ClockIn = ea.ClockIn.HasValue ? DateTime.Today.Add(ea.ClockIn.Value).ToString("hh:mm tt") : "-",
                ClockOut = ea.ClockOut.HasValue ? DateTime.Today.Add(ea.ClockOut.Value).ToString("hh:mm tt") : "-",
                ea.Status
            }).ToList();

            return Json(new { data });
        }

        [HttpGet]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> GetAttendance(int id)
        {
            var log = await _context.EmployeeAttendances.FindAsync(id);
            if (log == null) return NotFound();

            return Json(new
            {
                log.Id,
                log.EmployeeId,
                Date = log.Date.ToString("yyyy-MM-dd"),
                ClockIn = log.ClockIn?.ToString(@"hh\:mm") ?? "",
                ClockOut = log.ClockOut?.ToString(@"hh\:mm") ?? "",
                log.Status
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> SaveAttendance([FromBody] AttendanceInputModel model)
        {
            if (model.EmployeeId <= 0)
            {
                return Json(new { success = false, message = "Please select a valid employee." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            TimeSpan? clockInTime = null;
            if (TimeSpan.TryParse(model.ClockIn, out TimeSpan inTime)) clockInTime = inTime;

            TimeSpan? clockOutTime = null;
            if (TimeSpan.TryParse(model.ClockOut, out TimeSpan outTime)) clockOutTime = outTime;

            if (clockInTime.HasValue && clockOutTime.HasValue && clockInTime.Value >= clockOutTime.Value)
            {
                return Json(new { success = false, message = "Clock Out time must be after Clock In time." });
            }

            if (model.Id == 0)
            {
                // Check if attendance/leave already logged for employee on this date
                var existing = await _context.EmployeeAttendances.FirstOrDefaultAsync(ea => ea.EmployeeId == model.EmployeeId && ea.Date.Date == model.Date.Date);
                if (existing != null)
                {
                    if (!string.IsNullOrEmpty(existing.Status) && existing.Status.StartsWith("On Leave"))
                    {
                        return Json(new { success = false, message = $"An approved leave is already recorded for this employee on this date ({existing.Status}). Attendance cannot be overwritten directly. Please use the Edit button in the table if you wish to modify it." });
                    }

                    return Json(new { success = false, message = $"Attendance is already marked for this employee on this date (Status: {existing.Status}). Please use the Edit button in the table if you wish to modify it." });
                }

                var att = new EmployeeAttendance
                {
                    EmployeeId = model.EmployeeId,
                    Date = model.Date,
                    ClockIn = clockInTime,
                    ClockOut = clockOutTime,
                    Status = model.Status
                };
                _context.EmployeeAttendances.Add(att);
            }
            else
            {
                var att = await _context.EmployeeAttendances.FindAsync(model.Id);
                if (att == null) return Json(new { success = false, message = "Record not found." });

                // Check if another attendance record exists for this employee on this date
                var duplicate = await _context.EmployeeAttendances.AnyAsync(ea => ea.EmployeeId == model.EmployeeId && ea.Date.Date == model.Date.Date && ea.Id != model.Id);
                if (duplicate)
                {
                    return Json(new { success = false, message = "Another attendance record already exists for this employee on the selected date." });
                }

                att.EmployeeId = model.EmployeeId;
                att.Date = model.Date;
                att.ClockIn = clockInTime;
                att.ClockOut = clockOutTime;
                att.Status = model.Status;

                _context.Entry(att).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Attendance log saved successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var log = await _context.EmployeeAttendances.FindAsync(id);
            if (log == null) return Json(new { success = false, message = "Record not found." });

            _context.EmployeeAttendances.Remove(log);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Attendance log deleted." });
        }

        #endregion

        #region Leaves AJAX Endpoints

        [HttpGet]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> GetLeavesData()
        {
            var data = await _context.EmployeeLeaves
                .Include(el => el.Employee)
                .ThenInclude(e => e!.User)
                .Select(el => new
                {
                    el.Id,
                    el.EmployeeId,
                    EmployeeName = el.Employee != null && el.Employee.User != null ? el.Employee.User.FullName : "Unknown",
                    StartDate = el.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = el.EndDate.ToString("yyyy-MM-dd"),
                    el.LeaveType,
                    el.Status,
                    el.Notes
                })
                .ToListAsync();

            return Json(new { data });
        }

        [HttpGet]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> GetLeave(int id)
        {
            var leave = await _context.EmployeeLeaves.FindAsync(id);
            if (leave == null) return NotFound();

            return Json(new
            {
                leave.Id,
                leave.EmployeeId,
                StartDate = leave.StartDate.ToString("yyyy-MM-dd"),
                EndDate = leave.EndDate.ToString("yyyy-MM-dd"),
                leave.LeaveType,
                leave.Status,
                leave.Notes
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> SaveLeave([FromBody] EmployeeLeave model)
        {
            if (model.EmployeeId <= 0)
            {
                return Json(new { success = false, message = "Please select a valid employee." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            if (model.StartDate > model.EndDate)
            {
                return Json(new { success = false, message = "Start date must be before or equal to End date." });
            }

            // Check if an active/pending/approved leave request already exists for this employee covering the date range
            var hasOverlap = await _context.EmployeeLeaves.AnyAsync(l =>
                l.EmployeeId == model.EmployeeId &&
                l.Id != model.Id &&
                l.Status != "Rejected" &&
                l.StartDate.Date <= model.EndDate.Date &&
                l.EndDate.Date >= model.StartDate.Date);

            if (hasOverlap)
            {
                return Json(new { success = false, message = "A leave request already exists for this employee covering the selected date(s)." });
            }

            EmployeeLeave? existingLeave = null;
            if (model.Id != 0)
            {
                existingLeave = await _context.EmployeeLeaves.AsNoTracking().FirstOrDefaultAsync(l => l.Id == model.Id);
            }

            if (model.Id == 0)
            {
                _context.EmployeeLeaves.Add(model);
            }
            else
            {
                _context.Entry(model).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();

            // If existing leave was previously Approved, clear old attendance records first if dates/status changed
            if (existingLeave != null && string.Equals(existingLeave.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                var oldLogs = await _context.EmployeeAttendances
                    .Where(a => a.EmployeeId == existingLeave.EmployeeId 
                             && a.Date >= existingLeave.StartDate.Date 
                             && a.Date <= existingLeave.EndDate.Date 
                             && a.Status.StartsWith("On Leave"))
                    .ToListAsync();
                if (oldLogs.Any())
                {
                    _context.EmployeeAttendances.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();
                }
            }

            // Sync new leave status to attendance logs
            await SyncLeaveToAttendanceAsync(model);

            return Json(new { success = true, message = "Leave request saved successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> DeleteLeave(int id)
        {
            var leave = await _context.EmployeeLeaves.FindAsync(id);
            if (leave == null) return Json(new { success = false, message = "Leave request not found." });

            if (string.Equals(leave.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                var leaveLogs = await _context.EmployeeAttendances
                    .Where(a => a.EmployeeId == leave.EmployeeId 
                             && a.Date >= leave.StartDate.Date 
                             && a.Date <= leave.EndDate.Date 
                             && a.Status.StartsWith("On Leave"))
                    .ToListAsync();
                if (leaveLogs.Any())
                {
                    _context.EmployeeAttendances.RemoveRange(leaveLogs);
                }
            }

            _context.EmployeeLeaves.Remove(leave);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Leave request deleted." });
        }

        private async Task SyncLeaveToAttendanceAsync(EmployeeLeave leave)
        {
            var startDate = leave.StartDate.Date;
            var endDate = leave.EndDate.Date;
            var leaveStatusLabel = string.IsNullOrEmpty(leave.LeaveType) ? "On Leave" : $"On Leave ({leave.LeaveType})";

            if (string.Equals(leave.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                var existingAttendances = await _context.EmployeeAttendances
                    .Where(a => a.EmployeeId == leave.EmployeeId && a.Date >= startDate && a.Date <= endDate)
                    .ToListAsync();

                var existingMap = existingAttendances.ToDictionary(a => a.Date.Date);

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (existingMap.TryGetValue(date, out var att))
                    {
                        // Only overwrite status if there are no active manual clock-in/out times recorded
                        if (att.ClockIn == null && att.ClockOut == null)
                        {
                            att.Status = leaveStatusLabel;
                            _context.Entry(att).State = EntityState.Modified;
                        }
                    }
                    else
                    {
                        _context.EmployeeAttendances.Add(new EmployeeAttendance
                        {
                            EmployeeId = leave.EmployeeId,
                            Date = date,
                            ClockIn = null,
                            ClockOut = null,
                            Status = leaveStatusLabel
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                var autoLeaveLogs = await _context.EmployeeAttendances
                    .Where(a => a.EmployeeId == leave.EmployeeId && a.Date >= startDate && a.Date <= endDate && a.Status.StartsWith("On Leave"))
                    .ToListAsync();

                if (autoLeaveLogs.Any())
                {
                    _context.EmployeeAttendances.RemoveRange(autoLeaveLogs);
                    await _context.SaveChangesAsync();
                }
            }
        }

        #endregion

        #region Categories & Types AJAX Endpoints

        [HttpGet]
        [Authorize(Policy = "Inventory")]
        public async Task<IActionResult> GetCategoriesData()
        {
            var categories = await _context.ProductCategories
                .Include(c => c.TypeOptions)
                .ToListAsync();

            var data = categories.Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                TypeOptions = c.TypeOptions.Select(t => t.TypeName).ToList(),
                ProductCount = _context.Products.Count(p => p.CategoryName == c.Name)
            }).ToList();

            return Json(new { data });
        }

        [HttpGet]
        [Authorize(Policy = "Inventory")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.ProductCategories
                .Include(c => c.TypeOptions)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            return Json(new
            {
                category.Id,
                category.Name,
                category.Description,
                typeOptions = category.TypeOptions.Select(t => t.TypeName).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Inventory")]
        public async Task<IActionResult> SaveCategory([FromBody] CategoryInputModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return Json(new { success = false, message = "Category Name is required." });
            }

            model.Name = model.Name.Trim();

            var nameExists = await _context.ProductCategories.AnyAsync(c => c.Name == model.Name && c.Id != model.Id);
            if (nameExists)
            {
                return Json(new { success = false, message = $"Category '{model.Name}' already exists." });
            }

            // Clean up type options (distinct, non-empty, trimmed)
            var cleanTypes = (model.TypeOptions ?? new List<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (model.Id == 0)
            {
                var category = new ProductCategory
                {
                    Name = model.Name,
                    Description = model.Description?.Trim(),
                    TypeOptions = cleanTypes.Select(t => new ProductCategoryTypeOption { TypeName = t }).ToList()
                };

                _context.ProductCategories.Add(category);
            }
            else
            {
                var category = await _context.ProductCategories
                    .Include(c => c.TypeOptions)
                    .FirstOrDefaultAsync(c => c.Id == model.Id);

                if (category == null)
                {
                    return Json(new { success = false, message = "Category not found." });
                }

                // If category name changed, update existing products with old category name
                var oldCategoryName = category.Name;
                if (!string.Equals(oldCategoryName, model.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var linkedProducts = await _context.Products.Where(p => p.CategoryName == oldCategoryName).ToListAsync();
                    foreach (var p in linkedProducts)
                    {
                        p.CategoryName = model.Name;
                    }
                }

                category.Name = model.Name;
                category.Description = model.Description?.Trim();

                // Sync TypeOptions
                _context.ProductCategoryTypeOptions.RemoveRange(category.TypeOptions);
                category.TypeOptions = cleanTypes.Select(t => new ProductCategoryTypeOption
                {
                    ProductCategoryId = category.Id,
                    TypeName = t
                }).ToList();

                _context.Entry(category).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Category and dynamic types saved successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Inventory")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.ProductCategories
                .Include(c => c.TypeOptions)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return Json(new { success = false, message = "Category not found." });
            }

            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryName == category.Name);
            if (hasProducts)
            {
                return Json(new { success = false, message = $"Cannot delete category '{category.Name}' because there are products currently assigned to it in inventory. Please reassign or delete those products first." });
            }

            _context.ProductCategoryTypeOptions.RemoveRange(category.TypeOptions);
            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Category and its type options deleted successfully!" });
        }

        #endregion

        #region SMTP Settings AJAX Endpoints

        [HttpGet]
        [Authorize(Policy = "Settings")]
        public async Task<IActionResult> GetSmtpSettingsData()
        {
            var dbSmtp = await _context.SmtpSettings.OrderByDescending(s => s.Id).FirstOrDefaultAsync();

            if (dbSmtp != null)
            {
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        dbSmtp.Id,
                        dbSmtp.Server,
                        dbSmtp.Port,
                        dbSmtp.SenderName,
                        dbSmtp.SenderEmail,
                        dbSmtp.Username,
                        password = !string.IsNullOrEmpty(dbSmtp.Password) ? "••••••••••••" : "",
                        dbSmtp.EnableSsl,
                        isFromDatabase = true,
                        updatedAt = dbSmtp.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC")
                    }
                });
            }

            // Fallback to appsettings.json
            var fallback = _smtpFallbackOptions.Value;
            return Json(new
            {
                success = true,
                data = new
                {
                    id = 0,
                    server = fallback?.Server ?? "smtp.gmail.com",
                    port = fallback?.Port ?? 587,
                    senderName = fallback?.SenderName ?? "Inventory App",
                    senderEmail = fallback?.SenderEmail ?? "",
                    username = fallback?.Username ?? "",
                    password = !string.IsNullOrEmpty(fallback?.Password) ? "••••••••••••" : "",
                    enableSsl = fallback?.EnableSsl ?? true,
                    isFromDatabase = false,
                    updatedAt = "Not saved (Using appsettings.json defaults)"
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Settings")]
        public async Task<IActionResult> SaveSmtpSettings([FromBody] SmtpSetting model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            var existing = await _context.SmtpSettings.FirstOrDefaultAsync();

            string finalEncryptedPassword;
            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.StartsWith("••••"))
            {
                // Masked or empty password submitted -> retain existing password in DB
                finalEncryptedPassword = existing?.Password ?? string.Empty;
            }
            else
            {
                // New password typed -> encrypt before saving to DB
                finalEncryptedPassword = _protector.Protect(model.Password);
            }

            if (existing == null)
            {
                model.Password = finalEncryptedPassword;
                model.EnableSsl = true;
                model.UpdatedAt = DateTime.UtcNow;
                _context.SmtpSettings.Add(model);
            }
            else
            {
                existing.Server = model.Server?.Trim() ?? string.Empty;
                existing.Port = model.Port;
                existing.SenderName = model.SenderName?.Trim() ?? string.Empty;
                existing.SenderEmail = model.SenderEmail?.Trim() ?? string.Empty;
                existing.Username = model.Username?.Trim() ?? string.Empty;
                existing.Password = finalEncryptedPassword;
                existing.EnableSsl = true;
                existing.UpdatedAt = DateTime.UtcNow;

                _context.Entry(existing).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "SMTP Configuration saved securely!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Settings")]
        public async Task<IActionResult> TestSmtpConnection([FromBody] TestSmtpInputModel model)
        {
            if (string.IsNullOrWhiteSpace(model.TestEmail))
            {
                return Json(new { success = false, message = "Please enter a valid recipient email address for testing." });
            }

            if (string.IsNullOrWhiteSpace(model.Server) || string.IsNullOrWhiteSpace(model.SenderEmail))
            {
                return Json(new { success = false, message = "SMTP Server Host and Sender Email are required to send a test email." });
            }

            string testPassword = model.Password;
            if (string.IsNullOrWhiteSpace(testPassword) || testPassword.StartsWith("••••"))
            {
                // Unprotect existing encrypted password from DB
                var existing = await _context.SmtpSettings.FirstOrDefaultAsync();
                if (existing != null && !string.IsNullOrEmpty(existing.Password))
                {
                    try
                    {
                        testPassword = _protector.Unprotect(existing.Password);
                    }
                    catch
                    {
                        testPassword = existing.Password;
                    }
                }
                else
                {
                    testPassword = _smtpFallbackOptions.Value?.Password ?? string.Empty;
                }
            }

            try
            {
                using (var client = new System.Net.Mail.SmtpClient(model.Server, model.Port))
                {
                    if (!string.IsNullOrWhiteSpace(model.Username) || !string.IsNullOrWhiteSpace(testPassword))
                    {
                        client.Credentials = new System.Net.NetworkCredential(model.Username, testPassword);
                    }
                    client.EnableSsl = model.EnableSsl;
                    client.Timeout = 12000; // 12 seconds timeout for test

                    var mailMessage = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(model.SenderEmail, string.IsNullOrWhiteSpace(model.SenderName) ? "Inventory App" : model.SenderName),
                        Subject = "SMTP Dynamic Configuration Test",
                        Body = $@"
                            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                                <h3 style='color: #28a745;'>SMTP Test Connection Successful!</h3>
                                <p>Your SMTP settings in Inventory Management System are operating properly.</p>
                                <hr />
                                <p><strong>Server:</strong> {model.Server}:{model.Port}</p>
                                <p><strong>Sender Email:</strong> {model.SenderEmail}</p>
                                <p><strong>SSL Enabled:</strong> {(model.EnableSsl ? "Yes" : "No")}</p>
                                <p><strong>Tested At:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                            </div>",
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(model.TestEmail);

                    await client.SendMailAsync(mailMessage);
                }

                return Json(new { success = true, message = $"Test email successfully delivered to {model.TestEmail}!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"SMTP Connection Test Failed: {ex.Message}" });
            }
        }

        #endregion

        #region Company Profile Endpoints

        [HttpGet]
        [Authorize(Policy = "Settings")]
        public async Task<IActionResult> GetCompanyProfileData()
        {
            var profile = await _context.CompanyProfiles.FirstOrDefaultAsync();
            if (profile == null)
            {
                profile = new CompanyProfile();
            }
            return Json(new { success = true, data = profile });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Settings")]
        public async Task<IActionResult> SaveCompanyProfile([FromForm] CompanyProfile model, Microsoft.AspNetCore.Http.IFormFile? logoFile)
        {
            if (string.IsNullOrWhiteSpace(model.CompanyName))
            {
                return Json(new { success = false, message = "Company Name is required." });
            }

            var existing = await _context.CompanyProfiles.FirstOrDefaultAsync();
            if (existing == null)
            {
                existing = new CompanyProfile();
                _context.CompanyProfiles.Add(existing);
            }

            existing.CompanyName = model.CompanyName.Trim();
            existing.Tagline = (model.Tagline ?? "").Trim();
            existing.Address = (model.Address ?? "").Trim();
            existing.Phone = (model.Phone ?? "").Trim();
            existing.Email = (model.Email ?? "").Trim();
            existing.UpdatedAt = DateTime.Now;

            // Handle logo file upload if provided
            if (logoFile != null && logoFile.Length > 0)
            {
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg", ".gif", ".webp" };
                var ext = System.IO.Path.GetExtension(logoFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    return Json(new { success = false, message = "Invalid image file format. Supported: PNG, JPG, JPEG, SVG, WEBP." });
                }

                var webRoot = _webHostEnvironment.WebRootPath ?? System.IO.Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
                var uploadsFolder = System.IO.Path.Combine(webRoot, "uploads");
                if (!System.IO.Directory.Exists(uploadsFolder))
                {
                    System.IO.Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"company-logo_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                var filePath = System.IO.Path.Combine(uploadsFolder, fileName);

                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }

                existing.LogoPath = $"/uploads/{fileName}";
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Company Profile updated successfully!", profile = existing });
        }

        #endregion

    }
}
