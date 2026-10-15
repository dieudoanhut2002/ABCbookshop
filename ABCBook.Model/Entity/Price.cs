using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    [Table(nameof(Price))]
    public class Price
    {
        [Key]
        public Guid Id { get; set; }
        public Guid? ProductID { get; set; }
        public Guid? BookID { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
