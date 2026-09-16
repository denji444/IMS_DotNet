using System;

namespace InventoryManagementSystem.Models
{
    public class CompanyProfile
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = "Inventory Management System (IMS)";
        public string Tagline { get; set; } = "Smart Inventory, Sales & Enterprise Tracking";
        public string Address { get; set; } = "Main Commercial Boulevard, Business District";
        public string Phone { get; set; } = "+92 (300) 123-4567 | +92 (42) 3555-0199";
        public string Email { get; set; } = "info@imsportal.com | www.imsportal.com";
        public string? LogoPath { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
