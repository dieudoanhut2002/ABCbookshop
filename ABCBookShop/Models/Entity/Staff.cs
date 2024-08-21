using ABCBookShop.Models.Enum;

namespace ABCBookShop.Models.Entity
{
    public class Staff
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Sex sex { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime LabourContractDate { get; set; }
        public DateTime? QuitDate { get; set; }
        public Guid SalaryID { get; set; }
        public bool IsQuit { get; set; }
        public string Note { get; set; } = string.Empty;
        public StaffPosition StaffPosition { get; set; }

    }
}
