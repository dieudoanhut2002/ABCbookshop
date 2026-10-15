using ABCBook.Model.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Thực thể Chi nhánh / Cửa hàng trong chuỗi bán lẻ
    /// </summary>
    [Table(nameof(Store))]
    public class Store : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string OpeningHours { get; set; } = "08:00 - 22:00";

        // Navigation properties
        public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
        public virtual ICollection<POSRegister> Registers { get; set; } = new List<POSRegister>();
        public virtual ICollection<Staff> Staffs { get; set; } = new List<Staff>();
    }
}
