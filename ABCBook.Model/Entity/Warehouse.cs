using ABCBook.Model.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Kho hàng thuộc Cửa hàng hoặc Kho tổng chuỗi
    /// </summary>
    [Table(nameof(Warehouse))]
    public class Warehouse : AuditableEntity
    {
        public Guid StoreId { get; set; }
        public virtual Store? Store { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        public bool IsCentralWarehouse { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
        public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    }

    /// <summary>
    /// Quầy thu ngân / Thiết bị POS tại cửa hàng
    /// </summary>
    [Table(nameof(POSRegister))]
    public class POSRegister : AuditableEntity
    {
        public Guid StoreId { get; set; }
        public virtual Store? Store { get; set; }

        [Required]
        [MaxLength(50)]
        public string RegisterCode { get; set; } = string.Empty; // POS01, POS02

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public virtual ICollection<ShiftSession> ShiftSessions { get; set; } = new List<ShiftSession>();
    }
}
