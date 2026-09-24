namespace InventoryManagementSystem.Models.ViewModels
{
    public class ProductVariantInput
    {
        public string Sku { get; set; } = string.Empty;
        public string Variant { get; set; } = string.Empty;
        public string? Barcode { get; set; }
    }
}
