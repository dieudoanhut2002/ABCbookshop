using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBookShop.Models.Entity
{
    [Table(nameof(Salary))]
    public class Salary
    {
        [Key]
        public Guid Id { get; set; }
        public Guid StaffID { get; set; }
        public List<SalaryDetail> salaryDetails { get; set; } = new List<SalaryDetail>();
    }
    public class SalaryDetail
    {
        public decimal MonthlySalary { get; set; }
        public DateTime StartedDate { get; set; }
        public string Reason { get; set; }
    }
}
