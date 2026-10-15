using ABCBook.Model.Common;
using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Nhóm chi phí vận hành (Mặt bằng, điện nước, lương, bảo trì, marketing...)
    /// </summary>
    [Table(nameof(ExpenseCategory))]
    public class ExpenseCategory : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<ExpenseVoucher> Vouchers { get; set; } = new List<ExpenseVoucher>();
    }

    /// <summary>
    /// Phiếu chi phí vận hành phát sinh tại Cửa hàng (OPEX)
    /// </summary>
    [Table(nameof(ExpenseVoucher))]
    public class ExpenseVoucher : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string VoucherCode { get; set; } = string.Empty; // EXP-STORE1-202610-001

        public Guid StoreId { get; set; }
        public virtual Store? Store { get; set; }

        public Guid CategoryId { get; set; }
        public virtual ExpenseCategory? Category { get; set; }

        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(200)]
        public string Payee { get; set; } = string.Empty; // Người nhận tiền / Nhà cung cấp dịch vụ

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AttachmentUrl { get; set; } // Hóa đơn / Chứng từ đính kèm

        public ExpenseStatus Status { get; set; } = ExpenseStatus.Approved;

        public Guid? ApprovedByStaffId { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public bool IsRecurring { get; set; } = false; // Chi phí định kỳ (Tiền thuê mặt bằng, Internet...)
    }
}
