using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBookShop.Models.Entity
{
    [Table(nameof(Publisher))]
    public class Publisher
    {
        [Key]
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string ContactPerson { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }
        public required string Address { get; set; }
    }
}
