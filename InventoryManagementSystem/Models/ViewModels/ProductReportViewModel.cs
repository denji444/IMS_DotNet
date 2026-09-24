namespace InventoryManagementSystem.Models.ViewModels
{
    public class ProductReportViewModel
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Variant { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public int PurchasedQuantity { get; set; }
        public decimal PurchasedCost { get; set; }
        public int SoldQuantity { get; set; }
        public decimal SalesRevenue { get; set; }
        public decimal NetProfit => SalesRevenue - (PurchasedQuantity > 0 ? (PurchasedCost / PurchasedQuantity) * SoldQuantity : 0);
        public decimal CashFlow => SalesRevenue - PurchasedCost;
    }
}
