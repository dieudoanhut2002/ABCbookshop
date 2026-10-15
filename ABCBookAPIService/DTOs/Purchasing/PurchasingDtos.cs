using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace ABCBookAPIService.DTOs.Purchasing
{
    public class ProviderDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public decimal OutstandingDebt { get; set; }
        public bool IsActive { get; set; }
    }

    public class GoodsReceiptNoteDto
    {
        public Guid Id { get; set; }
        public string GRNCode { get; set; } = string.Empty;
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string SupplierInvoiceNo { get; set; } = string.Empty;
        public DateTime ReceivedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Note { get; set; } = string.Empty;
        public List<GoodsReceiptNoteItemDto> Items { get; set; } = new List<GoodsReceiptNoteItemDto>();
    }

    public class GoodsReceiptNoteItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductSKU { get; set; } = string.Empty;
        public string ProductTitle { get; set; } = string.Empty;
        public decimal ReceivedQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal => ReceivedQuantity * UnitCost;
    }

    public class GoodsReceiptNoteCreateDto
    {
        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
        public Guid ProviderId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn kho nhập hàng")]
        public Guid WarehouseId { get; set; }

        [MaxLength(100)]
        public string SupplierInvoiceNo { get; set; } = string.Empty;

        [Range(0, 10000000000, ErrorMessage = "Số tiền thanh toán ngay không hợp lệ")]
        public decimal PaidAmount { get; set; } = 0;

        [MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        public List<ReceiptItemInputDto> Items { get; set; } = new List<ReceiptItemInputDto>();
    }

    public class ReceiptItemInputDto
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}
