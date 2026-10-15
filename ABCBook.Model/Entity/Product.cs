using ABCBook.Model.Common;
using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Thực thể Sản phẩm tổng quát (Sách, Văn phòng phẩm, Quà lưu niệm, Hàng bán lẻ)
    /// </summary>
    [Table(nameof(Product))]
    public class Product : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [NotMapped]
        public string Name
        {
            get => Title;
            set => Title = value;
        }

        public string Description { get; set; } = string.Empty;

        public Guid? CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public ProductType ProductType { get; set; } = ProductType.Book;
        public bool IsBook { get; set; } = true;

        [MaxLength(30)]
        public string Unit { get; set; } = "Cuốn"; // Cuốn, Cái, Hộp, Bộ, Chiếc...

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; } = 0; // Giá vốn nhập kho

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; } = 0; // Giá bán lẻ niêm yết

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; } = 8; // Thuế VAT áp dụng (%)

        [Column(TypeName = "decimal(18,3)")]
        public decimal LowStockThreshold { get; set; } = 5; // Ngưỡng cảnh báo sắp hết hàng

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        // Legacy compatibility fields
        public Guid PriceID { get; set; } = Guid.Empty;
        public Guid ProviderID { get; set; } = Guid.Empty;
        public Guid StoreID { get; set; } = Guid.Empty;
        public Guid ShelvesLocationID { get; set; } = Guid.Empty;

        // Navigation properties
        public virtual BookDetail? BookDetail { get; set; }
        public virtual ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
        public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
