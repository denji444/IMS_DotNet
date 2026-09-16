using System;

namespace InventoryManagementSystem.Models.ViewModels
{
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
}
