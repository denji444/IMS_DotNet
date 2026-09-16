using System;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Models.ViewModels
{
    public class PayInstallmentModel
    {
        public int InstallmentId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public string? PaymentReference { get; set; }
        public string? BankName { get; set; }
        public DateTime? CheckDate { get; set; }
    }
}
