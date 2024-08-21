using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBookShop.Models.Entity
{
    [Table(nameof(Book))]
    public class Book
    {
        [Key]
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public bool IsSold { get; set; }
        public Guid StoreID { get; set; }
    }
}
