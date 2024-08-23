using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBookShop.Models.Entity
{
    /// <summary>
    /// Bảng quản lý vị trí hàng hóa trên các kệ hàng
    /// </summary>
    [Table(nameof(ShelvesLocation))]
    public class ShelvesLocation
    {
        [Key]
        public Guid Id { get; set; }
        /// <summary>
        /// Số kệ
        /// </summary>
        public required string ShelvesNo { get; set; }
        /// <summary>
        /// Số tầng trong kệ
        /// </summary>
        public int ShelvesLevel { get; set; }
        /// <summary>
        /// Vị trí cụ thể của hàng hóa/ sách trong tầng
        /// </summary>
        public required string ShelvesIndex { get; set; }
    }
}
