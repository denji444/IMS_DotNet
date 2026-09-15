using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class PurchaseItem
    {
        public int Id { get; set; }

        [Required]
        public int PurchaseId { get; set; }

        [ForeignKey(nameof(PurchaseId))]
        public Purchase? Purchase { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [StringLength(100)]
        public string? BatchNumber { get; set; }

        [Required]
        [Range(1, 1000000, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DemandRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FixRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }
    }
}
