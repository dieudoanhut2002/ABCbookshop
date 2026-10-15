using ABCBook.Model.Common;
using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Số lượng tồn kho tức thời tại từng kho
    /// Sử dụng Concurrency Token (RowVersion) để chống bán âm kho và race condition
    /// </summary>
    [Table(nameof(StockBalance))]
    public class StockBalance
    {
        public Guid WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        public Guid ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; } = 0;

        [Column(TypeName = "decimal(18,3)")]
        public decimal ReservedQuantity { get; set; } = 0; // Giữ hàng cho đơn online đang chờ thanh toán

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Sổ cái biến động tồn kho (Double-Entry Inventory Transaction Ledger)
    /// Bản ghi bất biến (Immutable) lưu vết toàn bộ lịch sử xuất - nhập - chuyển - kiểm kê
    /// </summary>
    [Table(nameof(StockTransaction))]
    public class StockTransaction : BaseEntity
    {
        public Guid WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        public Guid ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Required]
        [MaxLength(100)]
        public string DocumentNo { get; set; } = string.Empty; // Mã đơn POS, Mã GRN, Mã chuyển kho

        public StockTransactionType TransactionType { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; } // Dương = Nhập vào; Âm = Xuất ra

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; } = 0; // Giá vốn tại thời điểm giao dịch

        [Column(TypeName = "decimal(18,3)")]
        public decimal BeforeStock { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal AfterStock { get; set; }

        [MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        [MaxLength(100)]
        public string CreatedBy { get; set; } = "System";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
