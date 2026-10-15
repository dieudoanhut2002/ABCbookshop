using ABCBook.Model.Common;
using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Đơn luân chuyển hàng hóa giữa các kho / chi nhánh
    /// </summary>
    [Table(nameof(TransferOrder))]
    public class TransferOrder : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string TransferCode { get; set; } = string.Empty; // TRF-202610-0001

        public Guid FromWarehouseId { get; set; }
        public virtual Warehouse? FromWarehouse { get; set; }

        public Guid ToWarehouseId { get; set; }
        public virtual Warehouse? ToWarehouse { get; set; }

        public TransferStatus Status { get; set; } = TransferStatus.Draft;

        public DateTime? DispatchedAt { get; set; }
        [MaxLength(100)]
        public string? DispatchedBy { get; set; }

        public DateTime? ReceivedAt { get; set; }
        [MaxLength(100)]
        public string? ReceivedBy { get; set; }

        [MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        public virtual ICollection<TransferOrderItem> Items { get; set; } = new List<TransferOrderItem>();
    }

    [Table(nameof(TransferOrderItem))]
    public class TransferOrderItem : BaseEntity
    {
        public Guid TransferOrderId { get; set; }
        public virtual TransferOrder? TransferOrder { get; set; }

        public Guid ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal RequestedQuantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal DispatchedQuantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal ReceivedQuantity { get; set; }

        [MaxLength(200)]
        public string DiscrepancyNote { get; set; } = string.Empty;
    }
}
