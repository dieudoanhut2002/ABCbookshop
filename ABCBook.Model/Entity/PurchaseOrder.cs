using ABCBook.Model.Common;
using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Đơn đặt hàng mua từ Nhà cung cấp (Purchase Order - PO)
    /// </summary>
    [Table(nameof(PurchaseOrder))]
    public class PurchaseOrder : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string POCode { get; set; } = string.Empty; // PO-202610-0001

        public Guid ProviderId { get; set; }
        public virtual Provider? Provider { get; set; }

        public Guid StoreId { get; set; }
        public virtual Store? Store { get; set; }

        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedDeliveryDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        [MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
        public virtual ICollection<GoodsReceiptNote> GoodsReceiptNotes { get; set; } = new List<GoodsReceiptNote>();
    }

    [Table(nameof(PurchaseOrderItem))]
    public class PurchaseOrderItem : BaseEntity
    {
        public Guid PurchaseOrderId { get; set; }
        public virtual PurchaseOrder? PurchaseOrder { get; set; }

        public Guid ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal OrderedQuantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal ReceivedQuantity { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal => OrderedQuantity * UnitCost;
    }

    /// <summary>
    /// Phiếu nhập kho thực tế (Goods Receipt Note - GRN)
    /// </summary>
    [Table(nameof(GoodsReceiptNote))]
    public class GoodsReceiptNote : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string GRNCode { get; set; } = string.Empty; // GRN-202610-0001

        public Guid? PurchaseOrderId { get; set; }
        public virtual PurchaseOrder? PurchaseOrder { get; set; }

        public Guid ProviderId { get; set; }
        public virtual Provider? Provider { get; set; }

        public Guid WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        [MaxLength(100)]
        public string SupplierInvoiceNo { get; set; } = string.Empty;

        public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        public virtual ICollection<GoodsReceiptNoteItem> Items { get; set; } = new List<GoodsReceiptNoteItem>();
    }

    [Table(nameof(GoodsReceiptNoteItem))]
    public class GoodsReceiptNoteItem : BaseEntity
    {
        public Guid GoodsReceiptNoteId { get; set; }
        public virtual GoodsReceiptNote? GoodsReceiptNote { get; set; }

        public Guid ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal ReceivedQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal => ReceivedQuantity * UnitCost;
    }
}
