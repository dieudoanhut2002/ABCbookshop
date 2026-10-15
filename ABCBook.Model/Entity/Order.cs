using ABCBook.Model.Common;
using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Đơn hàng bán lẻ (POS hoặc E-commerce)
    /// </summary>
    [Table(nameof(Order))]
    public class Order : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty; // ORD-20261015-0001

        [Required]
        [MaxLength(100)]
        public string IdempotencyKey { get; set; } = string.Empty; // Chống double checkout

        public OrderSource Source { get; set; } = OrderSource.Pos;
        public OrderStatus Status { get; set; } = OrderStatus.Completed;

        public Guid StoreId { get; set; }
        public virtual Store? Store { get; set; }

        public Guid? ShiftSessionId { get; set; }
        public virtual ShiftSession? ShiftSession { get; set; }

        public Guid? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public Guid? StaffId { get; set; }
        public virtual Staff? Staff { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ChangeAmount { get; set; } = 0; // Tiền thối lại cho khách

        [MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public virtual ICollection<PaymentTransaction> Payments { get; set; } = new List<PaymentTransaction>();
    }

    [Table(nameof(OrderItem))]
    public class OrderItem : BaseEntity
    {
        public Guid OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public Guid ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Required]
        [MaxLength(250)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; } // Lưu lại giá vốn tại thời điểm bán để tính COGS & Lợi nhuận gộp

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount;
    }

    [Table(nameof(PaymentTransaction))]
    public class PaymentTransaction : BaseEntity
    {
        public Guid OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string TransactionReference { get; set; } = string.Empty; // Mã chuẩn chi thẻ / Mã giao dịch ngân hàng

        public bool IsSuccessful { get; set; } = true;
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    }
}
