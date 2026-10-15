using ABCBook.Model.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Thuộc tính chi tiết dành riêng cho mặt hàng Sách
    /// </summary>
    [Table(nameof(BookDetail))]
    public class BookDetail : BaseEntity
    {
        public Guid ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [MaxLength(20)]
        public string? ISBN { get; set; }

        [Required]
        [MaxLength(200)]
        public string Author { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Publisher { get; set; }

        public int? PublicationYear { get; set; }
        public int? PageCount { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; } = "Tiếng Việt";

        [MaxLength(50)]
        public string? CoverType { get; set; } = "Bìa mềm"; // Bìa mềm, Bìa cứng
    }
}
