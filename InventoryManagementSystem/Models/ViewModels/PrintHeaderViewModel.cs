namespace InventoryManagementSystem.Models
{
    public class PrintHeaderViewModel
    {
        public string DocumentTitle { get; set; } = "OFFICIAL DOCUMENT";
        public string DocumentNumberLabel { get; set; } = "Document No:";
        public string DocumentNumber { get; set; } = "";
        public string DateLabel { get; set; } = "Date:";
        public string DateValue { get; set; } = "";
        public string AdditionalInfo { get; set; } = "";
        public string CompanyName { get; set; } = "Inventory Management System (IMS)";
        public string Tagline { get; set; } = "Smart Inventory, Sales & Enterprise Tracking";
        public string Address { get; set; } = "Main Commercial Boulevard, Business District";
        public string Phone { get; set; } = "+92 (300) 123-4567 | +92 (42) 3555-0199";
        public string Email { get; set; } = "info@imsportal.com | www.imsportal.com";
        public string? LogoPath { get; set; } = "";
    }
}
