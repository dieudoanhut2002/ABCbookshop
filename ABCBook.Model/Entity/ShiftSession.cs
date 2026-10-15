using ABCBook.Model.Common;
using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Phiên ca làm việc của Thu ngân tại Quầy POS
    /// </summary>
    [Table(nameof(ShiftSession))]
    public class ShiftSession : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string ShiftCode { get; set; } = string.Empty; // SFT-STORE1-20261015-01

        public Guid StoreId { get; set; }
        public virtual Store? Store { get; set; }

        public Guid RegisterId { get; set; }
        public virtual POSRegister? Register { get; set; }

        public Guid StaffId { get; set; }
        public virtual Staff? Staff { get; set; }

        public ShiftStatus Status { get; set; } = ShiftStatus.Open;

        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningCash { get; set; } = 0; // Tiền mặt ban đầu trong két

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCashSales { get; set; } = 0; // Tổng doanh thu tiền mặt

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVietQrSales { get; set; } = 0; // Doanh thu VietQR

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCardSales { get; set; } = 0; // Doanh thu quẹt thẻ

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCashPaidOut { get; set; } = 0; // Chi tiền mặt tại quầy

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpectedCash { get; set; } = 0; // Opening + CashSales - PaidOut

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ActualCashCounted { get; set; } // Tiền mặt thực kiểm đếm khi đóng ca

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashDiscrepancy { get; set; } = 0; // Actual - Expected

        [MaxLength(500)]
        public string DiscrepancyReason { get; set; } = string.Empty; // Giải trình khi chênh lệch vượt ngưỡng

        public bool IsDiscrepancyApproved { get; set; } = false;
        public Guid? ApprovedByStaffId { get; set; }

        public int CompletedOrdersCount { get; set; } = 0;
        public int CancelledOrdersCount { get; set; } = 0;

        // Notification flags
        public bool IsReportSentTelegram { get; set; } = false;
        public bool IsReportSentEmail { get; set; } = false;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
