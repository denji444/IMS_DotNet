using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class Sale
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNo { get; set; } = string.Empty;

        [Display(Name = "Product")]
        public int? ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [Required(ErrorMessage = "Customer is required")]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [ForeignKey(nameof(CustomerId))]
        public ApplicationUser? Customer { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 1000000, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, 1000000.00, ErrorMessage = "Unit price must be greater than 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Notes { get; set; } = string.Empty;

        [StringLength(100)]
        public string? BatchNumber { get; set; }

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

        public System.Collections.Generic.ICollection<SaleInstallment> Installments { get; set; } = new System.Collections.Generic.List<SaleInstallment>();
        public System.Collections.Generic.ICollection<SaleItem> Items { get; set; } = new System.Collections.Generic.List<SaleItem>();

        [NotMapped]
        public string? NewCustomerFirstName { get; set; }
        [NotMapped]
        public string? NewCustomerLastName { get; set; }
        [NotMapped]
        public string? NewCustomerEmail { get; set; }
        [NotMapped]
        public string? NewCustomerPhone { get; set; }
        [NotMapped]
        public string? NewCustomerCnic { get; set; }
    }
}
