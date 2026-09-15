using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class Purchase
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PurchaseNo { get; set; } = string.Empty;

        [Display(Name = "Product")]
        public int? ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [Required(ErrorMessage = "Supplier is required")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public Supplier? Supplier { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 1000000, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, 1000000.00, ErrorMessage = "Unit price must be greater than 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Notes { get; set; } = string.Empty;

        [StringLength(100)]
        public string? BatchNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DemandRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FixRate { get; set; }

        [Required]
        public PaymentMode PaymentMode { get; set; } = PaymentMode.FullPayment;

        [Required]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [StringLength(100)]
        public string? PaymentReference { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        public DateTime? CheckDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DownPayment { get; set; } = 0.00m;

        public int InstallmentsCount { get; set; } = 0;

        [StringLength(50)]
        public string? InstallmentFrequency { get; set; } = "Monthly";

        public System.Collections.Generic.ICollection<PurchaseInstallment> Installments { get; set; } = new System.Collections.Generic.List<PurchaseInstallment>();
        public System.Collections.Generic.ICollection<PurchaseItem> Items { get; set; } = new System.Collections.Generic.List<PurchaseItem>();

        [NotMapped]
        public Supplier? NewSupplier { get; set; }
    }

    public enum PaymentMode
    {
        FullPayment = 0,
        Lease = 1
    }
}
