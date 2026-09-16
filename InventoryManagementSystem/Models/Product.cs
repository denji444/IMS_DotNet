using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using InventoryManagementSystem.Models.ViewModels;

namespace InventoryManagementSystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product SKU is required")]
        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
        [Remote("VerifySku", "Products", AdditionalFields = nameof(Id), ErrorMessage = "SKU already exists")]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product Name is required")]
        [StringLength(100, ErrorMessage = "Product Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Variant cannot exceed 100 characters")]
        public string Variant { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CategoryName { get; set; }

        [StringLength(100)]
        public string? ProductType { get; set; }

        [Range(0.00, 1000000.00, ErrorMessage = "Price must be non-negative")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, 1000000, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; } = 0;

        [NotMapped]
        public decimal? PurchaseRate { get; set; }

        [NotMapped]
        public decimal? DemandRate { get; set; }

        [NotMapped]
        public decimal? FixRate { get; set; }

        [NotMapped]
        public System.Collections.Generic.List<ProductVariantInput>? MultipleVariants { get; set; }
    }
}
