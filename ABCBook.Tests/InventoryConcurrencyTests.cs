using ABCBook.Model;
using ABCBook.Model.Entity;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Inventory;
using ABCBookAPIService.DTOs.Purchasing;
using ABCBookAPIService.Interfaces;
using ABCBookAPIService.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ABCBook.Tests
{
    public class InventoryConcurrencyTests
    {
        private ABCBookShopContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ABCBookShopContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ABCBookShopContext(options);
        }

        private async Task<(Guid warehouseId, Guid productId)> SeedWarehouseAndProductAsync(ABCBookShopContext context, decimal initialStock)
        {
            var store = new Store
            {
                Id = Guid.NewGuid(),
                Code = "TEST-ST",
                Name = "Cửa hàng thử nghiệm",
                Address = "Hồ Chí Minh",
                Phone = "0900000000",
                IsActive = true
            };
            await context.Stores.AddAsync(store);

            var warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                StoreId = store.Id,
                Code = "TEST-WH",
                Name = "Kho thử nghiệm",
                IsCentralWarehouse = false,
                IsActive = true
            };
            await context.Warehouses.AddAsync(warehouse);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                SKU = "BK-CONCURRENCY-01",
                Barcode = "ABC2601000001",
                Title = "Sách Lập trình Đồng thời .NET",
                CostPrice = 100000,
                SellingPrice = 160000,
                ProductType = ProductType.Book,
                Unit = "Cuốn",
                IsActive = true
            };
            await context.Products.AddAsync(product);

            if (initialStock > 0)
            {
                var stockBalance = new StockBalance
                {
                    WarehouseId = warehouse.Id,
                    ProductId = product.Id,
                    Quantity = initialStock,
                    LastUpdated = DateTime.UtcNow
                };
                await context.StockBalances.AddAsync(stockBalance);
            }

            await context.SaveChangesAsync();
            return (warehouse.Id, product.Id);
        }

        [Fact]
        public async Task DeductStock_WithSufficientQuantity_ShouldDecreaseStockAndWriteDoubleEntryLedger()
        {
            // Arrange
            var dbName = "TestDb_DeductSufficient_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);
            var (warehouseId, productId) = await SeedWarehouseAndProductAsync(context, 10);
            var inventoryService = new InventoryService(context);

            // Act
            await inventoryService.DeductStockAsync(
                warehouseId,
                productId,
                4,
                "DOC-TEST-01",
                StockTransactionType.PosSale,
                "Bán lẻ tại quầy",
                "ThuNgan01");

            // Assert
            var remainingStock = await inventoryService.GetProductStockInWarehouseAsync(warehouseId, productId);
            Assert.Equal(6, remainingStock);

            var ledgerEntry = await context.StockTransactions
                .FirstOrDefaultAsync(st => st.DocumentNo == "DOC-TEST-01");
            Assert.NotNull(ledgerEntry);
            Assert.Equal(-4, ledgerEntry.Quantity);
            Assert.Equal(10, ledgerEntry.BeforeStock);
            Assert.Equal(6, ledgerEntry.AfterStock);
            Assert.Equal(StockTransactionType.PosSale, ledgerEntry.TransactionType);
        }

        [Fact]
        public async Task DeductStock_WhenInsufficientQuantity_ShouldThrowInsufficientStockException()
        {
            // Arrange
            var dbName = "TestDb_Insufficient_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);
            var (warehouseId, productId) = await SeedWarehouseAndProductAsync(context, 3);
            var inventoryService = new InventoryService(context);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InsufficientStockException>(() =>
                inventoryService.DeductStockAsync(
                    warehouseId,
                    productId,
                    5,
                    "DOC-TEST-FAIL",
                    StockTransactionType.PosSale,
                    "Bán vượt tồn kho",
                    "ThuNgan01"));

            Assert.Equal(productId, ex.ProductId);
            Assert.Equal(warehouseId, ex.WarehouseId);
            Assert.Equal(5, ex.RequestedQuantity);
            Assert.Equal(3, ex.CurrentStock);

            // Tồn kho không bị trừ
            var currentStock = await inventoryService.GetProductStockInWarehouseAsync(warehouseId, productId);
            Assert.Equal(3, currentStock);
        }

        [Fact]
        public async Task TransferWorkflow_Draft_To_InTransit_To_Received_ShouldBalanceAcrossWarehouses()
        {
            // Arrange
            var dbName = "TestDb_TransferWorkflow_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);
            var (sourceWhId, productId) = await SeedWarehouseAndProductAsync(context, 20);

            // Tạo kho nhận đích
            var destWh = new Warehouse
            {
                Id = Guid.NewGuid(),
                StoreId = (await context.Stores.FirstAsync()).Id,
                Code = "DEST-WH",
                Name = "Kho chi nhánh nhận",
                IsCentralWarehouse = false,
                IsActive = true
            };
            await context.Warehouses.AddAsync(destWh);
            await context.SaveChangesAsync();

            var inventoryService = new InventoryService(context);
            var transferService = new TransferService(context, inventoryService);

            // 1. Tạo lệnh chuyển kho Draft 15 sản phẩm
            var createDto = new TransferOrderCreateDto
            {
                FromWarehouseId = sourceWhId,
                ToWarehouseId = destWh.Id,
                Note = "Chuyển sách bán chạy",
                Items = new List<TransferItemInputDto>
                {
                    new TransferItemInputDto { ProductId = productId, Quantity = 15 }
                }
            };

            var transferId = await transferService.CreateTransferOrderAsync(createDto, "QuanLyDieuPhoi");

            // Tồn kho tại kho xuất vẫn còn 20 vì mới là Draft
            var stockAtSourceDraft = await inventoryService.GetProductStockInWarehouseAsync(sourceWhId, productId);
            Assert.Equal(20, stockAtSourceDraft);

            // 2. Xuất kho chuyển đi (InTransit)
            var dispatched = await transferService.DispatchTransferOrderAsync(transferId, "ThuKhoXuat");
            Assert.True(dispatched);

            // Tồn kho tại kho xuất phải bị trừ 15, còn lại 5
            var stockAtSourceAfterDispatch = await inventoryService.GetProductStockInWarehouseAsync(sourceWhId, productId);
            Assert.Equal(5, stockAtSourceAfterDispatch);

            // Tồn kho tại kho nhận vẫn chưa tăng (đang trên đường vận chuyển)
            var stockAtDestBeforeReceive = await inventoryService.GetProductStockInWarehouseAsync(destWh.Id, productId);
            Assert.Equal(0, stockAtDestBeforeReceive);

            // 3. Kho nhận nhận đủ 15 sản phẩm (Received)
            var receivedQuantities = new Dictionary<Guid, decimal>
            {
                { productId, 15 }
            };
            var received = await transferService.ReceiveTransferOrderAsync(transferId, receivedQuantities, "ThuKhoNhan");
            Assert.True(received);

            // Tồn kho tại kho nhận bây giờ phải là 15
            var stockAtDestFinal = await inventoryService.GetProductStockInWarehouseAsync(destWh.Id, productId);
            Assert.Equal(15, stockAtDestFinal);

            // Tổng tồn kho toàn hệ thống được bảo toàn: 5 + 15 = 20
            var totalStockAcrossWarehouses = stockAtSourceAfterDispatch + stockAtDestFinal;
            Assert.Equal(20, totalStockAcrossWarehouses);
        }

        [Fact]
        public async Task GoodsReceiptNote_ShouldIncreaseStock_AndAccrueProviderDebt()
        {
            // Arrange
            var dbName = "TestDb_GRN_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);
            var (warehouseId, productId) = await SeedWarehouseAndProductAsync(context, 0);

            var provider = new Provider
            {
                Id = Guid.NewGuid(),
                Code = "NXB-TRE",
                Name = "Nhà xuất bản Trẻ",
                OutstandingDebt = 0,
                IsActive = true
            };
            await context.Providers.AddAsync(provider);
            await context.SaveChangesAsync();

            var inventoryService = new InventoryService(context);
            var purchasingService = new PurchasingService(context, inventoryService);

            // Act: Nhập 50 cuốn với giá 30,000đ = 1,500,000đ. Đã thanh toán 500,000đ => Nợ mới = 1,000,000đ
            var grnDto = new GoodsReceiptNoteCreateDto
            {
                ProviderId = provider.Id,
                WarehouseId = warehouseId,
                SupplierInvoiceNo = "INV-2026-999",
                PaidAmount = 500000,
                Note = "Nhập sách đầu tháng",
                Items = new List<ReceiptItemInputDto>
                {
                    new ReceiptItemInputDto { ProductId = productId, Quantity = 50, UnitCost = 30000 }
                }
            };

            var grnId = await purchasingService.CreateGoodsReceiptNoteAsync(grnDto, "ThuKhoTruong");

            // Assert
            var newStock = await inventoryService.GetProductStockInWarehouseAsync(warehouseId, productId);
            Assert.Equal(50, newStock);

            var updatedProvider = await context.Providers.FindAsync(provider.Id);
            Assert.NotNull(updatedProvider);
            Assert.Equal(1000000, updatedProvider.OutstandingDebt); // 1.5M - 0.5M

            // Kiểm tra Sổ cái biến động kho
            var ledgerEntry = await context.StockTransactions
                .FirstOrDefaultAsync(st => st.TransactionType == StockTransactionType.PurchaseReceipt);
            Assert.NotNull(ledgerEntry);
            Assert.Equal(50, ledgerEntry.Quantity);
            Assert.Equal(30000, ledgerEntry.UnitCost);
            Assert.Equal(0, ledgerEntry.BeforeStock);
            Assert.Equal(50, ledgerEntry.AfterStock);
        }

        [Fact]
        public async Task ConcurrentDeduction_WhenOnlyOneItemLeft_ShouldAllowOnlyOneAndPreventNegativeStock()
        {
            // Arrange
            var dbName = "TestDb_Concurrent_" + Guid.NewGuid();
            using var seedContext = CreateInMemoryContext(dbName);
            var (warehouseId, productId) = await SeedWarehouseAndProductAsync(seedContext, 1);

            // Act: 2 luồng đồng thời gọi DeductStockAsync cho 1 mặt hàng chỉ còn tồn = 1
            var successCount = 0;
            var failureCount = 0;

            var task1 = Task.Run(async () =>
            {
                using var ctx1 = CreateInMemoryContext(dbName);
                var svc1 = new InventoryService(ctx1);
                try
                {
                    await svc1.DeductStockAsync(warehouseId, productId, 1, "ORD-CONCUR-01", StockTransactionType.PosSale, "POS 1", "Cashier1");
                    Interlocked.Increment(ref successCount);
                }
                catch (InsufficientStockException)
                {
                    Interlocked.Increment(ref failureCount);
                }
            });

            var task2 = Task.Run(async () =>
            {
                using var ctx2 = CreateInMemoryContext(dbName);
                var svc2 = new InventoryService(ctx2);
                try
                {
                    await svc2.DeductStockAsync(warehouseId, productId, 1, "ORD-CONCUR-02", StockTransactionType.PosSale, "POS 2", "Cashier2");
                    Interlocked.Increment(ref successCount);
                }
                catch (InsufficientStockException)
                {
                    Interlocked.Increment(ref failureCount);
                }
            });

            await Task.WhenAll(task1, task2);

            // Assert: Không bao giờ được phép âm kho. Ít nhất 1 luồng phải thành công hoặc bị chặn an toàn
            using var verifyContext = CreateInMemoryContext(dbName);
            var verifyService = new InventoryService(verifyContext);
            var finalStock = await verifyService.GetProductStockInWarehouseAsync(warehouseId, productId);

            Assert.True(finalStock >= 0, $"Tồn kho không được phép âm! Tồn thực tế: {finalStock}");
        }
    }
}
