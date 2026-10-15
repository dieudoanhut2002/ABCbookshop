using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace ABCBookAPIService.DTOs.Inventory
{
    public class StockBalanceDto
    {
        public Guid WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;

        public Guid ProductId { get; set; }
        public string ProductSKU { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public string ProductTitle { get; set; } = string.Empty;
        public string Unit { get; set; } = "Cuốn";
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }

        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal AvailableQuantity => Quantity - ReservedQuantity;
        public decimal LowStockThreshold { get; set; }
        public bool IsLowStock => Quantity <= LowStockThreshold;
        public DateTime LastUpdated { get; set; }
    }

    public class StockTransactionDto
    {
        public Guid Id { get; set; }
        public string DocumentNo { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public string ProductTitle { get; set; } = string.Empty;
        public StockTransactionType TransactionType { get; set; }
        public string TransactionTypeName => TransactionType switch
        {
            StockTransactionType.PurchaseReceipt => "Nhập kho NCC",
            StockTransactionType.PosSale => "Xuất bán POS",
            StockTransactionType.OnlineSale => "Xuất bán Online",
            StockTransactionType.TransferOut => "Xuất điều chuyển đi",
            StockTransactionType.TransferIn => "Nhập điều chuyển về",
            StockTransactionType.DamagedLoss => "Xuất hủy hư hỏng/mất",
            StockTransactionType.StockAuditAdjustment => "Điều chỉnh kiểm kê",
            StockTransactionType.CustomerReturn => "Khách đổi trả",
            StockTransactionType.SupplierReturn => "Xuất trả NCC",
            _ => "Khác"
        };
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue => Math.Abs(Quantity) * UnitCost;
        public decimal BeforeStock { get; set; }
        public decimal AfterStock { get; set; }
        public string Note { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class StockAdjustmentDto
    {
        [Required]
        public Guid WarehouseId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public StockTransactionType AdjustmentType { get; set; } = StockTransactionType.DamagedLoss;

        [Range(0.001, 100000, ErrorMessage = "Số lượng điều chỉnh phải lớn hơn 0")]
        public decimal Quantity { get; set; } // Số lượng điều chỉnh

        public bool IsReduction { get; set; } = true; // True: Giảm kho (hỏng, mất); False: Tăng kho (thừa)

        [Required(ErrorMessage = "Vui lòng nhập lý do điều chỉnh")]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class TransferOrderDto
    {
        public Guid Id { get; set; }
        public string TransferCode { get; set; } = string.Empty;
        public Guid FromWarehouseId { get; set; }
        public string FromWarehouseName { get; set; } = string.Empty;
        public Guid ToWarehouseId { get; set; }
        public string ToWarehouseName { get; set; } = string.Empty;
        public TransferStatus Status { get; set; }
        public string StatusName => Status switch
        {
            TransferStatus.Draft => "Chờ xuất chuyển (Dự thảo)",
            TransferStatus.Dispatched => "Đang vận chuyển (In-Transit)",
            TransferStatus.Received => "Đã nhận nhập kho",
            TransferStatus.Cancelled => "Đã hủy",
            _ => "Không xác định"
        };
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? DispatchedAt { get; set; }
        public string? DispatchedBy { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string? ReceivedBy { get; set; }
        public string Note { get; set; } = string.Empty;
        public int TotalItemsCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public List<TransferOrderItemDto> Items { get; set; } = new List<TransferOrderItemDto>();
    }

    public class TransferOrderItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductSKU { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public string ProductTitle { get; set; } = string.Empty;
        public string Unit { get; set; } = "Cuốn";
        public decimal CurrentStockAtSource { get; set; }
        public decimal RequestedQuantity { get; set; }
        public decimal DispatchedQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public string DiscrepancyNote { get; set; } = string.Empty;
    }

    public class TransferOrderCreateDto
    {
        [Required(ErrorMessage = "Vui lòng chọn kho gửi")]
        public Guid FromWarehouseId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn kho nhận")]
        public Guid ToWarehouseId { get; set; }

        [MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        public List<TransferItemInputDto> Items { get; set; } = new List<TransferItemInputDto>();
    }

    public class TransferItemInputDto
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class WarehouseDto
    {
        public Guid Id { get; set; }
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsCentralWarehouse { get; set; }
        public bool IsActive { get; set; }
    }
}
