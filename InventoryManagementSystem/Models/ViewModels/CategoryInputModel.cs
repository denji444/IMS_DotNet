using System.Collections.Generic;

namespace InventoryManagementSystem.Models.ViewModels
{
    public class CategoryInputModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> TypeOptions { get; set; } = new List<string>();
    }
}
