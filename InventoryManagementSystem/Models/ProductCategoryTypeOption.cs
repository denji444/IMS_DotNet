using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class ProductCategoryTypeOption
    {
        public int Id { get; set; }

        public int ProductCategoryId { get; set; }
        public ProductCategory? Category { get; set; }

        [Required]
        [StringLength(100)]
        public string TypeName { get; set; } = string.Empty;
    }
}
