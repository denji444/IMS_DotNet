using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Exceptions;

namespace InventoryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MasterSettingsController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MasterSettingsController(InventoryDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
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

            ViewBag.DepartmentList = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
            return View();
        }

        #region Departments AJAX Endpoints

        [HttpGet]
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
        public async Task<IActionResult> GetDepartment(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();
            return Json(dept);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        public class EmployeeInputModel
        {
            public int Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public int? DepartmentId { get; set; }
            public string Designation { get; set; } = string.Empty;
            public DateTime HireDate { get; set; }
            public decimal Salary { get; set; }
            public string Status { get; set; } = "Active";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        #endregion

        #region Attendance AJAX Endpoints

        [HttpGet]
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

        public class AttendanceInputModel
        {
            public int Id { get; set; }
            public int EmployeeId { get; set; }
            public DateTime Date { get; set; }
            public string? ClockIn { get; set; }
            public string? ClockOut { get; set; }
            public string Status { get; set; } = "Present";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

    }
}
