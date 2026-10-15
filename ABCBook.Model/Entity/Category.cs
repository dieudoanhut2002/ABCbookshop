using ABCBook.Model.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Danh mục sản phẩm (Hỗ trợ cấu trúc phân cấp danh mục cha - con)
    /// </summary>
    [Table(nameof(Category))]
    public class Category : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public Guid? ParentId { get; set; }
        public virtual Category? Parent { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;

        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
