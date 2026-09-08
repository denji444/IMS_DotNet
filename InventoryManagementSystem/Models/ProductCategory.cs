using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class ProductCategory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category Name is required")]
        [StringLength(100, ErrorMessage = "Category Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        public ICollection<ProductCategoryTypeOption> TypeOptions { get; set; } = new List<ProductCategoryTypeOption>();
    }

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
