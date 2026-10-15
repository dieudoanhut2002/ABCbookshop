using ABCBook.Model.Common;
using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Khách hàng & Thành viên thân thiết
    /// </summary>
    [Table(nameof(Customer))]
    public class Customer : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string CustomerCode { get; set; } = string.Empty; // CUST-0001

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSpending { get; set; } = 0; // Tổng chi tiêu tích lũy

        public int RewardPoints { get; set; } = 0; // Điểm thưởng khả dụng hiện tại

        [MaxLength(50)]
        public string MembershipTier { get; set; } = "Standard"; // Standard, Silver, Gold, Platinum

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<LoyaltyPointLedger> LoyaltyPointLedgers { get; set; } = new List<LoyaltyPointLedger>();
    }

    /// <summary>
    /// Sổ cái điểm thưởng thành viên (Lưu vết mọi giao dịch tích/đổi điểm)
    /// </summary>
    [Table(nameof(LoyaltyPointLedger))]
    public class LoyaltyPointLedger : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public Guid? OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public LoyaltyTransactionType TransactionType { get; set; }

        public int PointsChanged { get; set; } // Dương: tích điểm; Âm: đổi điểm
        public int BalanceAfter { get; set; }

        [MaxLength(250)]
        public string Reason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
