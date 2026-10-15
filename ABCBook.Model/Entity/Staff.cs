using ABCBook.Model.Common;
using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Bảng quản lý nhân viên các chi nhánh
    /// </summary>
    [Table(nameof(Staff))]
    public class Staff : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string StaffCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public Guid StoreID { get; set; }
        public virtual Store? Store { get; set; }

        public Guid? UserId { get; set; }
        public virtual AppUser? User { get; set; }

        public Gender gender { get; set; } = Gender.Male;
        public DateTime DateOfBirth { get; set; }
        public DateTime LabourContractDate { get; set; } = DateTime.UtcNow;
        public DateTime? QuitDate { get; set; }
        public bool IsQuit { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseSalary { get; set; } = 0;

        public string Note { get; set; } = string.Empty;
        public StaffPosition StaffPosition { get; set; } = StaffPosition.Assistant;
        public Guid SalaryID { get; set; } = Guid.Empty;

        public virtual ICollection<ShiftSession> ShiftSessions { get; set; } = new List<ShiftSession>();
    }
}
