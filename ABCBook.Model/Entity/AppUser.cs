using ABCBook.Model.Common;
using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Tài khoản xác thực người dùng trong hệ thống
    /// </summary>
    [Table(nameof(AppUser))]
    public class AppUser : AuditableEntity
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;

        public SystemRole Role { get; set; } = SystemRole.SalesStaff;

        public Guid? AssignedStoreId { get; set; } // Null = Chain scope (Sys Chain Owner, Chain Manager)
        public virtual Store? AssignedStore { get; set; }

        public bool IsActive { get; set; } = true;

        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public virtual Staff? Staff { get; set; }
    }
}
