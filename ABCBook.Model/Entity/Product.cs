using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    [Table(nameof(Product))]
    public class Product
    {
        [Key]
        public Guid Id { get; set; }
        public Guid PriceID { get; set; }
        public Guid ProviderID { get; set; }
        public Guid StoreID { get; set; }
        public Guid ShelvesLocationID { get; set; }
        public required string Title { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
