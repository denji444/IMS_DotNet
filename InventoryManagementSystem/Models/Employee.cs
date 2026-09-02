using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        [Required(ErrorMessage = "Designation is required")]
        [StringLength(100, ErrorMessage = "Designation cannot exceed 100 characters")]
        public string Designation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hire Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Hire Date")]
        public DateTime HireDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Salary is required")]
        [Range(0.00, 10000000.00, ErrorMessage = "Salary must be non-negative")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Inactive, On Leave, Terminated
    }
}
