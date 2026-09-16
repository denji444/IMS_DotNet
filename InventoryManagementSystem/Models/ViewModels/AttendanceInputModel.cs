using System;

namespace InventoryManagementSystem.Models.ViewModels
{
    public class AttendanceInputModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public string? ClockIn { get; set; }
        public string? ClockOut { get; set; }
        public string Status { get; set; } = "Present";
    }
}
