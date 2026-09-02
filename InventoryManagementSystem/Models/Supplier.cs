using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Supplier Name is required")]
        [StringLength(100, ErrorMessage = "Supplier Name cannot exceed 100 characters")]
        [Remote(action: "VerifyName", controller: "Suppliers", AdditionalFields = nameof(Id))]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Name is required")]
        [StringLength(100, ErrorMessage = "Contact Name cannot exceed 100 characters")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
        [Phone(ErrorMessage = "Invalid Phone Number")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters")]
        public string Address { get; set; } = string.Empty;

        public bool IsEmailVerified { get; set; } = false;
        public string? EmailVerificationToken { get; set; }

        public string? UserId { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }
    }
}
