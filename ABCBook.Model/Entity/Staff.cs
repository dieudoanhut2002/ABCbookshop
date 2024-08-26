using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Bảng quản lý nhân viên các chi nhánh
    /// </summary>
    [Table(nameof(Staff))]
    public class Staff
    {
        [Key]
        public Guid Id { get; set; }
        public Guid SalaryID { get; set; }
        public Guid StoreID { get; set; }
        public required string Name { get; set; }
        public Gender gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime LabourContractDate { get; set; }
        public DateTime? QuitDate { get; set; }
        public bool IsQuit { get; set; }
        public string Note { get; set; } = string.Empty;
        public StaffPosition StaffPosition { get; set; }

    }
}
