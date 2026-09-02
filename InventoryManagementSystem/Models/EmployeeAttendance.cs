using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class EmployeeAttendance
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Display(Name = "Clock In")]
        public TimeSpan? ClockIn { get; set; }

        [Display(Name = "Clock Out")]
        public TimeSpan? ClockOut { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Present"; // Present, Absent, Late, Half Day
    }
}
