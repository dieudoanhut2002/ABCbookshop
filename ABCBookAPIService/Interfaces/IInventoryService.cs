using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.DTOs.Inventory;
using ABCBookAPIService.DTOs.Purchasing;

namespace ABCBookAPIService.Interfaces
{
    public interface IInventoryService
    {
        Task<List<WarehouseDto>> GetWarehousesAsync(Guid? storeId = null);
        Task<PagedResult<StockBalanceDto>> GetStockBalancesAsync(Guid? warehouseId, string? keyword, bool? onlyLowStock, int pageIndex, int pageSize);
        Task<PagedResult<StockTransactionDto>> GetStockTransactionsAsync(Guid? warehouseId, Guid? productId, DateTime? fromDate, DateTime? toDate, int pageIndex, int pageSize);
        Task<decimal> GetProductStockInWarehouseAsync(Guid warehouseId, Guid productId);

        // Trừ kho có Concurrency Control & Database Transaction
        Task DeductStockAsync(Guid warehouseId, Guid productId, decimal quantity, string documentNo, StockTransactionType transactionType, string note, string operatorName);

        // Cộng kho có Concurrency Control & Database Transaction
        Task AddStockAsync(Guid warehouseId, Guid productId, decimal quantity, decimal unitCost, string documentNo, StockTransactionType transactionType, string note, string operatorName);

        // Điều chỉnh kiểm kê hoặc xuất hủy hàng hỏng/mất
        Task<bool> AdjustStockAsync(StockAdjustmentDto dto, string operatorName);
    }

    public interface ITransferService
    {
        Task<List<TransferOrderDto>> GetTransferOrdersAsync(Guid? fromWarehouseId = null, Guid? toWarehouseId = null, TransferStatus? status = null);
        Task<TransferOrderDto?> GetTransferOrderByIdAsync(Guid id);
        Task<Guid> CreateTransferOrderAsync(TransferOrderCreateDto dto, string operatorName);
        Task<bool> DispatchTransferOrderAsync(Guid transferOrderId, string operatorName);
        Task<bool> ReceiveTransferOrderAsync(Guid transferOrderId, Dictionary<Guid, decimal> receivedQuantities, string operatorName);
        Task<bool> CancelTransferOrderAsync(Guid transferOrderId, string operatorName);
    }

    public interface IPurchasingService
    {
        Task<List<ProviderDto>> GetProvidersAsync();
        Task<Guid> CreateProviderAsync(ProviderDto dto, string operatorName);
        Task<List<GoodsReceiptNoteDto>> GetGoodsReceiptNotesAsync(Guid? warehouseId = null);
        Task<Guid> CreateGoodsReceiptNoteAsync(GoodsReceiptNoteCreateDto dto, string operatorName);
    }

    public class InsufficientStockException : Exception
    {
        public Guid ProductId { get; }
        public Guid WarehouseId { get; }
        public decimal RequestedQuantity { get; }
        public decimal CurrentStock { get; }

        public InsufficientStockException(Guid productId, Guid warehouseId, decimal requested, decimal current, string productName)
            : base($"Không đủ tồn kho cho sản phẩm '{productName}'. Yêu cầu xuất: {requested}, tồn kho khả dụng hiện tại: {current}.")
        {
            ProductId = productId;
            WarehouseId = warehouseId;
            RequestedQuantity = requested;
            CurrentStock = current;
        }
    }
}
