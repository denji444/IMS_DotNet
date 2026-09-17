using System.Collections.Generic;

namespace InventoryManagementSystem.Models.ViewModels
{
    public class EmployeePermissionsModel
    {
        public int EmployeeId { get; set; }
        public List<string> Permissions { get; set; } = new();
    }
}
