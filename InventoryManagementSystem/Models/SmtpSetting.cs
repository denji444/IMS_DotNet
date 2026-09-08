using System;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class SmtpSetting
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "SMTP Server host is required")]
        [StringLength(200)]
        public string Server { get; set; } = string.Empty;

        [Required]
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
        public int Port { get; set; } = 587;

        [Required(ErrorMessage = "Sender Name is required")]
        [StringLength(100)]
        public string SenderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sender Email is required")]
        [EmailAddress]
        [StringLength(150)]
        public string SenderEmail { get; set; } = string.Empty;

        [StringLength(150)]
        public string Username { get; set; } = string.Empty;

        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        public bool EnableSsl { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
