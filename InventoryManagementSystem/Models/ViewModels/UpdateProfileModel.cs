namespace InventoryManagementSystem.Models.ViewModels
{
    public class UpdateProfileModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}
