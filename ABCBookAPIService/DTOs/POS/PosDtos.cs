using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace ABCBookAPIService.DTOs.POS
{
    public class ShiftOpenDto
    {
        [Required(ErrorMessage = "Vui lòng chọn Cửa hàng")]
        public Guid StoreId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Quầy thu ngân (POS Register)")]
        public Guid RegisterId { get; set; }

        public Guid? StaffId { get; set; }

        [Range(0, 100000000, ErrorMessage = "Tiền đầu ca không hợp lệ")]
        public decimal OpeningCash { get; set; } = 0; // Số tiền mặt ban đầu trong két
    }

    public class ShiftCloseDto
    {
        [Required]
        public Guid ShiftId { get; set; }

        [Range(0, 1000000000, ErrorMessage = "Số tiền kiểm đếm thực tế không hợp lệ")]
        public decimal ActualCashCounted { get; set; }

        [MaxLength(500)]
        public string? DiscrepancyReason { get; set; }
    }

    public class ShiftDetailDto
    {
        public Guid Id { get; set; }
        public string ShiftCode { get; set; } = string.Empty;
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public Guid RegisterId { get; set; }
        public string RegisterCode { get; set; } = string.Empty;
        public string RegisterName { get; set; } = string.Empty;
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public ShiftStatus Status { get; set; }
        public string StatusName => Status switch
        {
            ShiftStatus.Open => "Đang mở (Bán hàng)",
            ShiftStatus.Closed => "Đã đóng (Chờ duyệt)",
            ShiftStatus.Approved => "Đã duyệt",
            _ => "Không xác định"
        };
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal TotalCashSales { get; set; }
        public decimal TotalVietQrSales { get; set; }
        public decimal TotalCardSales { get; set; }
        public decimal TotalCashPaidOut { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal? ActualCashCounted { get; set; }
        public decimal CashDiscrepancy { get; set; }
        public string DiscrepancyReason { get; set; } = string.Empty;
        public int CompletedOrdersCount { get; set; }
        public decimal TotalRevenue => TotalCashSales + TotalVietQrSales + TotalCardSales;
    }

    public class PosProductSearchDto
    {
        public Guid Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public string Unit { get; set; } = "Cuốn";
        public decimal AvailableStock { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class PosCheckoutDto
    {
        [Required]
        public Guid ShiftSessionId { get; set; }

        [Required]
        public Guid StoreId { get; set; }

        [Required]
        public Guid WarehouseId { get; set; }

        public Guid? CustomerId { get; set; }

        [Required]
        public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString();

        public List<PosOrderItemDto> Items { get; set; } = new List<PosOrderItemDto>();

        public decimal DiscountAmount { get; set; } = 0; // Giảm giá trực tiếp trên đơn

        public int PointsToRedeem { get; set; } = 0; // Đổi điểm thưởng (1 pt = 1,000 VND)

        public List<PosPaymentDto> Payments { get; set; } = new List<PosPaymentDto>();

        [MaxLength(500)]
        public string? Note { get; set; }
    }

    public class PosOrderItemDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(0.001, 10000, ErrorMessage = "Số lượng mua không hợp lệ")]
        public decimal Quantity { get; set; } = 1;

        public decimal UnitPrice { get; set; }

        public decimal DiscountAmount { get; set; } = 0;
    }

    public class PosPaymentDto
    {
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [Range(0.01, 1000000000, ErrorMessage = "Số tiền thanh toán phải lớn hơn 0")]
        public decimal Amount { get; set; }

        public string? TransactionReference { get; set; } // Mã GD ngân hàng / Mã thẻ
    }

    public class PosOrderReceiptDto
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string StoreAddress { get; set; } = string.Empty;
        public string StorePhone { get; set; } = string.Empty;
        public string RegisterCode { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public int CustomerPointsEarned { get; set; }
        public int CustomerCurrentPoints { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LoyaltyDiscount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }

        public List<PosReceiptItemDto> Items { get; set; } = new List<PosReceiptItemDto>();
        public List<PosPaymentDto> Payments { get; set; } = new List<PosPaymentDto>();
    }

    public class PosReceiptItemDto
    {
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineTotal { get; set; }
    }
}
