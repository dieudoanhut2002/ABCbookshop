using ABCBook.Model.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Nhật ký kiểm toán bảo mật và giao dịch nhạy cảm
    /// </summary>
    [Table(nameof(AuditLog))]
    public class AuditLog : BaseEntity
    {
        public Guid? UserId { get; set; }

        [MaxLength(100)]
        public string UserName { get; set; } = "Anonymous";

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty; // LOGIN, CHANGE_ROLE, UPDATE_PRICE, STOCK_ADJUST, CLOSE_SHIFT_DISCREPANCY, APPROVE_EXPENSE

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityId { get; set; }

        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
