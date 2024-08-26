using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Quản lý lịch sử lương của các nhân viên
    /// </summary>
    [Table(nameof(Salary))]
    public class Salary
    {
        [Key]
        public Guid Id { get; set; }
        public Guid StaffID { get; set; }
        public decimal MonthlySalary { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
