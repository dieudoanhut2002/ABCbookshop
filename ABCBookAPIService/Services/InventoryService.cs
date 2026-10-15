using ABCBook.Model;
using ABCBook.Model.Entity;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.DTOs.Inventory;
using ABCBookAPIService.DTOs.Purchasing;
using ABCBookAPIService.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ABCBookAPIService.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ABCBookShopContext _context;

        public InventoryService(ABCBookShopContext context)
        {
            _context = context;
        }

        public async Task<List<WarehouseDto>> GetWarehousesAsync(Guid? storeId = null)
        {
            var query = _context.Warehouses
                .Include(w => w.Store)
                .Where(w => !w.IsDeleted && w.IsActive);

            if (storeId.HasValue)
            {
                query = query.Where(w => w.StoreId == storeId.Value);
            }

            return await query
                .OrderBy(w => w.Store!.Name)
                .ThenBy(w => w.Name)
                .Select(w => new WarehouseDto
                {
                    Id = w.Id,
                    StoreId = w.StoreId,
                    StoreName = w.Store != null ? w.Store.Name : "",
                    Code = w.Code,
                    Name = w.Name,
                    IsCentralWarehouse = w.IsCentralWarehouse,
                    IsActive = w.IsActive
                })
                .ToListAsync();
        }

        public async Task<PagedResult<StockBalanceDto>> GetStockBalancesAsync(Guid? warehouseId, string? keyword, bool? onlyLowStock, int pageIndex, int pageSize)
        {
            var query = _context.StockBalances
                .Include(sb => sb.Warehouse)
                    .ThenInclude(w => w!.Store)
                .Include(sb => sb.Product)
                .AsQueryable();

            if (warehouseId.HasValue)
            {
                query = query.Where(sb => sb.WarehouseId == warehouseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                query = query.Where(sb =>
                    sb.Product!.Title.Contains(kw) ||
                    sb.Product.SKU.Contains(kw) ||
                    sb.Product.Barcode.Contains(kw) ||
                    sb.Warehouse!.Name.Contains(kw));
            }

            if (onlyLowStock == true)
            {
                query = query.Where(sb => sb.Quantity <= sb.Product!.LowStockThreshold);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(sb => sb.Warehouse!.Name)
                .ThenBy(sb => sb.Product!.Title)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(sb => new StockBalanceDto
                {
                    WarehouseId = sb.WarehouseId,
                    WarehouseCode = sb.Warehouse!.Code,
                    WarehouseName = sb.Warehouse.Name,
                    StoreName = sb.Warehouse.Store != null ? sb.Warehouse.Store.Name : "",
                    ProductId = sb.ProductId,
                    ProductSKU = sb.Product!.SKU,
                    ProductBarcode = sb.Product.Barcode,
                    ProductTitle = sb.Product.Title,
                    Unit = sb.Product.Unit,
                    CostPrice = sb.Product.CostPrice,
                    SellingPrice = sb.Product.SellingPrice,
                    Quantity = sb.Quantity,
                    ReservedQuantity = sb.ReservedQuantity,
                    LowStockThreshold = sb.Product.LowStockThreshold,
                    LastUpdated = sb.LastUpdated
                })
                .ToListAsync();

            return new PagedResult<StockBalanceDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<StockTransactionDto>> GetStockTransactionsAsync(Guid? warehouseId, Guid? productId, DateTime? fromDate, DateTime? toDate, int pageIndex, int pageSize)
        {
            var query = _context.StockTransactions
                .Include(st => st.Warehouse)
                .Include(st => st.Product)
                .AsQueryable();

            if (warehouseId.HasValue)
            {
                query = query.Where(st => st.WarehouseId == warehouseId.Value);
            }

            if (productId.HasValue)
            {
                query = query.Where(st => st.ProductId == productId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(st => st.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(st => st.CreatedAt <= toDate.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(st => st.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(st => new StockTransactionDto
                {
                    Id = st.Id,
                    DocumentNo = st.DocumentNo,
                    WarehouseName = st.Warehouse != null ? st.Warehouse.Name : "",
                    ProductSKU = st.Product != null ? st.Product.SKU : "",
                    ProductTitle = st.Product != null ? st.Product.Title : "",
                    TransactionType = st.TransactionType,
                    Quantity = st.Quantity,
                    UnitCost = st.UnitCost,
                    BeforeStock = st.BeforeStock,
                    AfterStock = st.AfterStock,
                    Note = st.Note,
                    CreatedBy = st.CreatedBy,
                    CreatedAt = st.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<StockTransactionDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<decimal> GetProductStockInWarehouseAsync(Guid warehouseId, Guid productId)
        {
            var balance = await _context.StockBalances
                .FirstOrDefaultAsync(sb => sb.WarehouseId == warehouseId && sb.ProductId == productId);

            return balance?.Quantity ?? 0;
        }

        public async Task DeductStockAsync(Guid warehouseId, Guid productId, decimal quantity, string documentNo, StockTransactionType transactionType, string note, string operatorName)
        {
            if (quantity <= 0) throw new ArgumentException("Số lượng xuất kho phải lớn hơn 0.", nameof(quantity));

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId)
                ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm có ID: {productId}");

            var balance = await _context.StockBalances
                .FirstOrDefaultAsync(sb => sb.WarehouseId == warehouseId && sb.ProductId == productId);

            decimal currentQty = balance?.Quantity ?? 0;

            if (currentQty < quantity)
            {
                throw new InsufficientStockException(productId, warehouseId, quantity, currentQty, product.Title);
            }

            decimal beforeStock = balance!.Quantity;
            balance.Quantity -= quantity;
            balance.LastUpdated = DateTime.UtcNow;

            var transaction = new StockTransaction
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                ProductId = productId,
                DocumentNo = documentNo,
                TransactionType = transactionType,
                Quantity = -quantity, // Âm vì xuất kho
                UnitCost = product.CostPrice,
                BeforeStock = beforeStock,
                AfterStock = balance.Quantity,
                Note = note,
                CreatedBy = operatorName,
                CreatedAt = DateTime.UtcNow
            };

            await _context.StockTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task AddStockAsync(Guid warehouseId, Guid productId, decimal quantity, decimal unitCost, string documentNo, StockTransactionType transactionType, string note, string operatorName)
        {
            if (quantity <= 0) throw new ArgumentException("Số lượng nhập kho phải lớn hơn 0.", nameof(quantity));

            var balance = await _context.StockBalances
                .FirstOrDefaultAsync(sb => sb.WarehouseId == warehouseId && sb.ProductId == productId);

            decimal beforeStock = 0;

            if (balance == null)
            {
                balance = new StockBalance
                {
                    WarehouseId = warehouseId,
                    ProductId = productId,
                    Quantity = quantity,
                    LastUpdated = DateTime.UtcNow
                };
                await _context.StockBalances.AddAsync(balance);
            }
            else
            {
                beforeStock = balance.Quantity;
                balance.Quantity += quantity;
                balance.LastUpdated = DateTime.UtcNow;
            }

            var transaction = new StockTransaction
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                ProductId = productId,
                DocumentNo = documentNo,
                TransactionType = transactionType,
                Quantity = quantity, // Dương vì nhập kho
                UnitCost = unitCost,
                BeforeStock = beforeStock,
                AfterStock = balance.Quantity,
                Note = note,
                CreatedBy = operatorName,
                CreatedAt = DateTime.UtcNow
            };

            await _context.StockTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AdjustStockAsync(StockAdjustmentDto dto, string operatorName)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId);
            if (product == null) return false;

            var docNo = $"ADJ-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 9999)}";

            if (dto.IsReduction)
            {
                await DeductStockAsync(
                    dto.WarehouseId,
                    dto.ProductId,
                    dto.Quantity,
                    docNo,
                    dto.AdjustmentType,
                    dto.Reason,
                    operatorName);
            }
            else
            {
                await AddStockAsync(
                    dto.WarehouseId,
                    dto.ProductId,
                    dto.Quantity,
                    product.CostPrice,
                    docNo,
                    dto.AdjustmentType,
                    dto.Reason,
                    operatorName);
            }

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserName = operatorName,
                Action = "STOCK_ADJUSTMENT",
                EntityName = nameof(StockBalance),
                EntityId = $"{dto.WarehouseId}_{dto.ProductId}",
                NewValues = $"Document: {docNo}, Type: {dto.AdjustmentType}, Qty: {(dto.IsReduction ? "-" : "+")}{dto.Quantity}, Reason: {dto.Reason}",
                Timestamp = DateTime.UtcNow
            };
            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();

            return true;
        }
    }

    public class TransferService : ITransferService
    {
        private readonly ABCBookShopContext _context;
        private readonly IInventoryService _inventoryService;

        public TransferService(ABCBookShopContext context, IInventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
        }

        public async Task<List<TransferOrderDto>> GetTransferOrdersAsync(Guid? fromWarehouseId = null, Guid? toWarehouseId = null, TransferStatus? status = null)
        {
            var query = _context.TransferOrders
                .Include(t => t.FromWarehouse)
                .Include(t => t.ToWarehouse)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Product)
                .Where(t => !t.IsDeleted);

            if (fromWarehouseId.HasValue) query = query.Where(t => t.FromWarehouseId == fromWarehouseId.Value);
            if (toWarehouseId.HasValue) query = query.Where(t => t.ToWarehouseId == toWarehouseId.Value);
            if (status.HasValue) query = query.Where(t => t.Status == status.Value);

            return await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TransferOrderDto
                {
                    Id = t.Id,
                    TransferCode = t.TransferCode,
                    FromWarehouseId = t.FromWarehouseId,
                    FromWarehouseName = t.FromWarehouse != null ? t.FromWarehouse.Name : "",
                    ToWarehouseId = t.ToWarehouseId,
                    ToWarehouseName = t.ToWarehouse != null ? t.ToWarehouse.Name : "",
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    CreatedBy = t.CreatedBy,
                    DispatchedAt = t.DispatchedAt,
                    DispatchedBy = t.DispatchedBy,
                    ReceivedAt = t.ReceivedAt,
                    ReceivedBy = t.ReceivedBy,
                    Note = t.Note,
                    TotalItemsCount = t.Items.Count,
                    TotalQuantity = t.Items.Sum(i => i.RequestedQuantity)
                })
                .ToListAsync();
        }

        public async Task<TransferOrderDto?> GetTransferOrderByIdAsync(Guid id)
        {
            var t = await _context.TransferOrders
                .Include(t => t.FromWarehouse)
                .Include(t => t.ToWarehouse)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (t == null) return null;

            var items = new List<TransferOrderItemDto>();
            foreach (var item in t.Items)
            {
                var stockAtSource = await _inventoryService.GetProductStockInWarehouseAsync(t.FromWarehouseId, item.ProductId);
                items.Add(new TransferOrderItemDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductSKU = item.Product?.SKU ?? "",
                    ProductBarcode = item.Product?.Barcode ?? "",
                    ProductTitle = item.Product?.Title ?? "",
                    Unit = item.Product?.Unit ?? "Cuốn",
                    CurrentStockAtSource = stockAtSource,
                    RequestedQuantity = item.RequestedQuantity,
                    DispatchedQuantity = item.DispatchedQuantity,
                    ReceivedQuantity = item.ReceivedQuantity,
                    DiscrepancyNote = item.DiscrepancyNote
                });
            }

            return new TransferOrderDto
            {
                Id = t.Id,
                TransferCode = t.TransferCode,
                FromWarehouseId = t.FromWarehouseId,
                FromWarehouseName = t.FromWarehouse?.Name ?? "",
                ToWarehouseId = t.ToWarehouseId,
                ToWarehouseName = t.ToWarehouse?.Name ?? "",
                Status = t.Status,
                CreatedAt = t.CreatedAt,
                CreatedBy = t.CreatedBy,
                DispatchedAt = t.DispatchedAt,
                DispatchedBy = t.DispatchedBy,
                ReceivedAt = t.ReceivedAt,
                ReceivedBy = t.ReceivedBy,
                Note = t.Note,
                TotalItemsCount = t.Items.Count,
                TotalQuantity = t.Items.Sum(i => i.RequestedQuantity),
                Items = items
            };
        }

        public async Task<Guid> CreateTransferOrderAsync(TransferOrderCreateDto dto, string operatorName)
        {
            if (dto.FromWarehouseId == dto.ToWarehouseId)
            {
                throw new InvalidOperationException("Kho gửi và kho nhận không được trùng nhau.");
            }

            if (dto.Items == null || !dto.Items.Any())
            {
                throw new InvalidOperationException("Vui lòng chọn ít nhất một mặt hàng để điều chuyển.");
            }

            var transferCode = $"TRF-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 9999)}";

            var order = new TransferOrder
            {
                Id = Guid.NewGuid(),
                TransferCode = transferCode,
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
                Status = TransferStatus.Draft,
                Note = dto.Note?.Trim() ?? string.Empty,
                CreatedBy = operatorName,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0) continue;
                order.Items.Add(new TransferOrderItem
                {
                    Id = Guid.NewGuid(),
                    TransferOrderId = order.Id,
                    ProductId = item.ProductId,
                    RequestedQuantity = item.Quantity,
                    DispatchedQuantity = 0,
                    ReceivedQuantity = 0
                });
            }

            await _context.TransferOrders.AddAsync(order);
            await _context.SaveChangesAsync();
            return order.Id;
        }

        public async Task<bool> DispatchTransferOrderAsync(Guid transferOrderId, string operatorName)
        {
            var order = await _context.TransferOrders
                .Include(t => t.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(t => t.Id == transferOrderId && !t.IsDeleted);

            if (order == null || order.Status != TransferStatus.Draft)
            {
                throw new InvalidOperationException("Đơn chuyển kho không tồn tại hoặc đã được xử lý xuất kho trước đó.");
            }

            // Trừ tồn kho tại kho xuất gửi
            foreach (var item in order.Items)
            {
                await _inventoryService.DeductStockAsync(
                    order.FromWarehouseId,
                    item.ProductId,
                    item.RequestedQuantity,
                    order.TransferCode,
                    StockTransactionType.TransferOut,
                    $"Xuất chuyển kho đến chi nhánh đích ({order.TransferCode})",
                    operatorName);

                item.DispatchedQuantity = item.RequestedQuantity;
            }

            order.Status = TransferStatus.Dispatched;
            order.DispatchedAt = DateTime.UtcNow;
            order.DispatchedBy = operatorName;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = operatorName;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReceiveTransferOrderAsync(Guid transferOrderId, Dictionary<Guid, decimal> receivedQuantities, string operatorName)
        {
            var order = await _context.TransferOrders
                .Include(t => t.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(t => t.Id == transferOrderId && !t.IsDeleted);

            if (order == null || order.Status != TransferStatus.Dispatched)
            {
                throw new InvalidOperationException("Đơn chuyển kho chưa ở trạng thái 'Đang vận chuyển (In-Transit)'.");
            }

            // Cộng tồn kho tại kho đích
            foreach (var item in order.Items)
            {
                var received = receivedQuantities.TryGetValue(item.ProductId, out var qty) ? qty : item.DispatchedQuantity;
                item.ReceivedQuantity = Math.Max(0, received);

                if (item.ReceivedQuantity > 0)
                {
                    await _inventoryService.AddStockAsync(
                        order.ToWarehouseId,
                        item.ProductId,
                        item.ReceivedQuantity,
                        item.Product?.CostPrice ?? 0,
                        order.TransferCode,
                        StockTransactionType.TransferIn,
                        $"Nhận chuyển kho từ chi nhánh gửi ({order.TransferCode})",
                        operatorName);
                }

                // Nếu có hao hụt (nhận thiếu)
                if (item.ReceivedQuantity < item.DispatchedQuantity)
                {
                    var loss = item.DispatchedQuantity - item.ReceivedQuantity;
                    item.DiscrepancyNote = $"Hao hụt/thất thoát trong quá trình vận chuyển: {loss} {item.Product?.Unit ?? "Cuốn"}";
                }
            }

            order.Status = TransferStatus.Received;
            order.ReceivedAt = DateTime.UtcNow;
            order.ReceivedBy = operatorName;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = operatorName;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelTransferOrderAsync(Guid transferOrderId, string operatorName)
        {
            var order = await _context.TransferOrders
                .FirstOrDefaultAsync(t => t.Id == transferOrderId && !t.IsDeleted);

            if (order == null || order.Status != TransferStatus.Draft)
            {
                throw new InvalidOperationException("Chỉ có thể hủy đơn chuyển kho khi đang ở trạng thái 'Dự thảo'.");
            }

            order.Status = TransferStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = operatorName;

            await _context.SaveChangesAsync();
            return true;
        }
    }

    public class PurchasingService : IPurchasingService
    {
        private readonly ABCBookShopContext _context;
        private readonly IInventoryService _inventoryService;

        public PurchasingService(ABCBookShopContext context, IInventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
        }

        public async Task<List<ProviderDto>> GetProvidersAsync()
        {
            return await _context.Providers
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Name)
                .Select(p => new ProviderDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    ContactPerson = p.ContactPerson,
                    Phone = p.Phone,
                    Email = p.Email,
                    Address = p.Address,
                    TaxCode = p.TaxCode,
                    OutstandingDebt = p.OutstandingDebt,
                    IsActive = p.IsActive
                })
                .ToListAsync();
        }

        public async Task<Guid> CreateProviderAsync(ProviderDto dto, string operatorName)
        {
            var code = dto.Code.Trim().ToUpper();
            var exists = await _context.Providers.AnyAsync(p => p.Code == code && !p.IsDeleted);
            if (exists)
            {
                throw new InvalidOperationException($"Mã nhà cung cấp '{code}' đã tồn tại.");
            }

            var provider = new Provider
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = dto.Name.Trim(),
                ContactPerson = dto.ContactPerson?.Trim() ?? string.Empty,
                Phone = dto.Phone?.Trim() ?? string.Empty,
                Email = dto.Email?.Trim() ?? string.Empty,
                Address = dto.Address?.Trim() ?? string.Empty,
                TaxCode = dto.TaxCode?.Trim() ?? string.Empty,
                OutstandingDebt = 0,
                IsActive = true,
                CreatedBy = operatorName,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Providers.AddAsync(provider);
            await _context.SaveChangesAsync();
            return provider.Id;
        }

        public async Task<List<GoodsReceiptNoteDto>> GetGoodsReceiptNotesAsync(Guid? warehouseId = null)
        {
            var query = _context.GoodsReceiptNotes
                .Include(g => g.Provider)
                .Include(g => g.Warehouse)
                .Include(g => g.Items)
                    .ThenInclude(i => i.Product)
                .Where(g => !g.IsDeleted);

            if (warehouseId.HasValue) query = query.Where(g => g.WarehouseId == warehouseId.Value);

            return await query
                .OrderByDescending(g => g.ReceivedDate)
                .Select(g => new GoodsReceiptNoteDto
                {
                    Id = g.Id,
                    GRNCode = g.GRNCode,
                    ProviderId = g.ProviderId,
                    ProviderName = g.Provider != null ? g.Provider.Name : "",
                    WarehouseId = g.WarehouseId,
                    WarehouseName = g.Warehouse != null ? g.Warehouse.Name : "",
                    SupplierInvoiceNo = g.SupplierInvoiceNo,
                    ReceivedDate = g.ReceivedDate,
                    TotalAmount = g.TotalAmount,
                    PaidAmount = g.PaidAmount,
                    Note = g.Note,
                    Items = g.Items.Select(i => new GoodsReceiptNoteItemDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductSKU = i.Product != null ? i.Product.SKU : "",
                        ProductTitle = i.Product != null ? i.Product.Title : "",
                        ReceivedQuantity = i.ReceivedQuantity,
                        UnitCost = i.UnitCost
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<Guid> CreateGoodsReceiptNoteAsync(GoodsReceiptNoteCreateDto dto, string operatorName)
        {
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Id == dto.ProviderId && !p.IsDeleted)
                ?? throw new KeyNotFoundException("Không tìm thấy Nhà cung cấp.");

            var grnCode = $"GRN-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 9999)}";
            decimal totalAmount = 0;

            var grn = new GoodsReceiptNote
            {
                Id = Guid.NewGuid(),
                GRNCode = grnCode,
                ProviderId = dto.ProviderId,
                WarehouseId = dto.WarehouseId,
                SupplierInvoiceNo = dto.SupplierInvoiceNo?.Trim() ?? string.Empty,
                ReceivedDate = DateTime.UtcNow,
                PaidAmount = dto.PaidAmount,
                Note = dto.Note?.Trim() ?? string.Empty,
                CreatedBy = operatorName,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0) continue;
                var lineTotal = item.Quantity * item.UnitCost;
                totalAmount += lineTotal;

                grn.Items.Add(new GoodsReceiptNoteItem
                {
                    Id = Guid.NewGuid(),
                    GoodsReceiptNoteId = grn.Id,
                    ProductId = item.ProductId,
                    ReceivedQuantity = item.Quantity,
                    UnitCost = item.UnitCost
                });

                // Tự động cộng tồn kho vào Kho đích thông qua Sổ cái kép
                await _inventoryService.AddStockAsync(
                    dto.WarehouseId,
                    item.ProductId,
                    item.Quantity,
                    item.UnitCost,
                    grnCode,
                    StockTransactionType.PurchaseReceipt,
                    $"Nhập hàng từ NCC '{provider.Name}' theo phiếu {grnCode}",
                    operatorName);
            }

            grn.TotalAmount = totalAmount;

            // Ghi nhận công nợ NCC: Công nợ tăng thêm = (Tổng tiền - Đã thanh toán)
            decimal debtIncrease = totalAmount - dto.PaidAmount;
            if (debtIncrease > 0)
            {
                provider.OutstandingDebt += debtIncrease;
            }

            await _context.GoodsReceiptNotes.AddAsync(grn);
            await _context.SaveChangesAsync();

            return grn.Id;
        }
    }
}
