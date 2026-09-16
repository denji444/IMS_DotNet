namespace InventoryManagementSystem.Models.ViewModels
{
    public class CustomerInputModel
    {
        public string? Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Cnic { get; set; }
    }
}
