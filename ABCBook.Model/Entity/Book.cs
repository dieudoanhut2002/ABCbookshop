using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    /// <summary>
    /// Bảng quản lý danh sách các sách
    /// </summary>
    [Table(nameof(Book))]
    public class Book
    {
        [Key]
        public Guid Id { get; set; }
        public Guid PriceID { get; set; }
        public Guid PublisherID { get; set; }
        public Guid StoreID { get; set; }
        public Guid ShelvesLocationID { get; set; }
        public required string Title { get; set; }
        public string Description { get; set; } = string.Empty;
        public required string Author { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public Discount? Discount { get; set; }
        public bool IsSold { get; set; } = false;
    }


}
