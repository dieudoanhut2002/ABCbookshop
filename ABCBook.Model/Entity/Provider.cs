using ABCBook.Model.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Nhà cung cấp sách / hàng hóa
    /// </summary>
    [Table(nameof(Provider))]
    public class Provider : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ContactPerson { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(50)]
        public string TaxCode { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingDebt { get; set; } = 0; // Công nợ hiện tại phải trả

        public bool IsActive { get; set; } = true;

        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public virtual ICollection<GoodsReceiptNote> GoodsReceiptNotes { get; set; } = new List<GoodsReceiptNote>();
    }
}
