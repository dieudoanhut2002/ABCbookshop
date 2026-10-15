using ABCBook.Model;
using ABCBook.Model.Entity;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.POS;
using ABCBookAPIService.Interfaces;
using ABCBookAPIService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ABCBook.Tests
{
    public class PosShiftTests
    {
        private ABCBookShopContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ABCBookShopContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ABCBookShopContext(options);
        }

        private async Task<(Store store, Warehouse warehouse, POSRegister register, Staff staff, Product product)> SeedPosEnvironmentAsync(ABCBookShopContext context, decimal productStock = 50)
        {
            var store = new Store
            {
                Id = Guid.NewGuid(),
                Code = "STORE-HCM01",
                Name = "Nhà sách ABC Quận 1",
                Address = "123 Lê Lợi, Q1, TP.HCM",
                Phone = "028 1234 5678",
                IsActive = true
            };
            await context.Stores.AddAsync(store);

            var warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                StoreId = store.Id,
                Code = "WH-HCM01",
                Name = "Kho quầy bán lẻ Q1",
                IsCentralWarehouse = false,
                IsActive = true
            };
            await context.Warehouses.AddAsync(warehouse);

            var register = new POSRegister
            {
                Id = Guid.NewGuid(),
                StoreId = store.Id,
                RegisterCode = "POS-01",
                Name = "Quầy Thu Ngân 01",
                IsActive = true
            };
            await context.POSRegisters.AddAsync(register);

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                StaffCode = "NV001",
                Name = "Nguyễn Thu Ngân",
                StoreID = store.Id,
                IsQuit = false
            };
            await context.Staffs.AddAsync(staff);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                SKU = "BK-DPT-01",
                Barcode = "ABC2601990001",
                Title = "Đắc Nhân Tâm",
                CostPrice = 50000,
                SellingPrice = 86000,
                ProductType = ProductType.Book,
                Unit = "Cuốn",
                IsActive = true
            };
            await context.Products.AddAsync(product);

            if (productStock > 0)
            {
                await context.StockBalances.AddAsync(new StockBalance
                {
                    WarehouseId = warehouse.Id,
                    ProductId = product.Id,
                    Quantity = productStock,
                    LastUpdated = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            return (store, warehouse, register, staff, product);
        }

        [Fact]
        public async Task OpenShift_ShouldSetExpectedCashToOpeningCash_AndMarkOpen()
        {
            // Arrange
            var dbName = "TestDb_OpenShift_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);
            var (store, _, register, staff, _) = await SeedPosEnvironmentAsync(context);
            var shiftService = new ShiftService(context, NullLogger<ShiftService>.Instance);

            var openDto = new ShiftOpenDto
            {
                StoreId = store.Id,
                RegisterId = register.Id,
                StaffId = staff.Id,
                OpeningCash = 1500000
            };

            // Act
            var shiftId = await shiftService.OpenShiftAsync(openDto, staff.Name);

            // Assert
            var shift = await shiftService.GetShiftByIdAsync(shiftId);
            Assert.NotNull(shift);
            Assert.Equal(ShiftStatus.Open, shift.Status);
            Assert.Equal(1500000, shift.OpeningCash);
            Assert.Equal(1500000, shift.ExpectedCash);
            Assert.Equal(0, shift.TotalCashSales);
            Assert.StartsWith($"SFT-{store.Code}", shift.ShiftCode);
        }

        [Fact]
        public async Task OpenShift_WhenRegisterAlreadyHasOpenShift_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var dbName = "TestDb_OpenShift_Dup_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);
            var (store, _, register, staff, _) = await SeedPosEnvironmentAsync(context);
            var shiftService = new ShiftService(context, NullLogger<ShiftService>.Instance);

            var openDto = new ShiftOpenDto
            {
                StoreId = store.Id,
                RegisterId = register.Id,
                StaffId = staff.Id,
                OpeningCash = 1000000
            };

            await shiftService.OpenShiftAsync(openDto, staff.Name);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                shiftService.OpenShiftAsync(openDto, staff.Name));
        }

        [Fact]
        public async Task ProcessCheckout_ShouldDeductStock_IncreaseShiftSales_AndEarnCustomerPoints()
        {
            // Arrange
            var dbName = "TestDb_Checkout_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);
            var (store, warehouse, register, staff, product) = await SeedPosEnvironmentAsync(context, 10);

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                CustomerCode = "KH-001",
                FullName = "Lê Thị Khách",
                PhoneNumber = "0987654321",
                RewardPoints = 50,
                TotalSpending = 500000,
                IsActive = true
            };
            await context.Customers.AddAsync(customer);
            await context.SaveChangesAsync();

            var shiftService = new ShiftService(context, NullLogger<ShiftService>.Instance);
            var inventoryService = new InventoryService(context);
            var posService = new PosService(context, inventoryService, NullLogger<PosService>.Instance);

            var shiftId = await shiftService.OpenShiftAsync(new ShiftOpenDto
            {
                StoreId = store.Id,
                RegisterId = register.Id,
                StaffId = staff.Id,
                OpeningCash = 1000000
            }, staff.Name);

            // Act: Mua 2 cuốn Đắc Nhân Tâm (2 x 86,000 = 172,000đ). Đổi 20 điểm (= 20,000đ). Tổng trả: 152,000đ
            var checkoutDto = new PosCheckoutDto
            {
                ShiftSessionId = shiftId,
                StoreId = store.Id,
                WarehouseId = warehouse.Id,
                CustomerId = customer.Id,
                PointsToRedeem = 20,
                IdempotencyKey = "TEST-IDEM-01",
                Items = new List<PosOrderItemDto>
                {
                    new PosOrderItemDto { ProductId = product.Id, Quantity = 2, UnitPrice = 86000, DiscountAmount = 0 }
                },
                Payments = new List<PosPaymentDto>
                {
                    new PosPaymentDto { PaymentMethod = PaymentMethod.Cash, Amount = 200000 } // Khách đưa 200,000đ tiền mặt
                }
            };

            var receipt = await posService.ProcessCheckoutAsync(checkoutDto, staff.Name);

            // Assert Receipt
            Assert.NotNull(receipt);
            Assert.Equal(172000, receipt.SubTotal);
            Assert.Equal(152000, receipt.TotalAmount); // 172,000 - 20,000
            Assert.Equal(48000, receipt.ChangeAmount); // 200,000 - 152,000
            Assert.Equal(15, receipt.CustomerPointsEarned); // 152,000 / 10,000 = 15 points

            // Assert Inventory deducted atomically via ledger
            var remainingStock = await inventoryService.GetProductStockInWarehouseAsync(warehouse.Id, product.Id);
            Assert.Equal(8, remainingStock); // 10 - 2 = 8

            // Assert Customer points: 50 - 20 + 15 = 45 points
            var updatedCustomer = await context.Customers.FindAsync(customer.Id);
            Assert.NotNull(updatedCustomer);
            Assert.Equal(45, updatedCustomer.RewardPoints);

            // Assert Shift session updated
            var shift = await shiftService.GetShiftByIdAsync(shiftId);
            Assert.NotNull(shift);
            Assert.Equal(152000, shift.TotalCashSales); // Thực thu tiền mặt = 200k - 48k = 152k
            Assert.Equal(1152000, shift.ExpectedCash); // 1,000,000 + 152,000
            Assert.Equal(1, shift.CompletedOrdersCount);
        }

        [Fact]
        public async Task ProcessCheckout_WithIdempotencyKey_ShouldPreventDuplicateOrder()
        {
            // Arrange
            var dbName = "TestDb_Idempotency_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);
            var (store, warehouse, register, staff, product) = await SeedPosEnvironmentAsync(context, 10);

            var shiftService = new ShiftService(context, NullLogger<ShiftService>.Instance);
            var inventoryService = new InventoryService(context);
            var posService = new PosService(context, inventoryService, NullLogger<PosService>.Instance);

            var shiftId = await shiftService.OpenShiftAsync(new ShiftOpenDto
            {
                StoreId = store.Id,
                RegisterId = register.Id,
                StaffId = staff.Id,
                OpeningCash = 1000000
            }, staff.Name);

            var checkoutDto = new PosCheckoutDto
            {
                ShiftSessionId = shiftId,
                StoreId = store.Id,
                WarehouseId = warehouse.Id,
                IdempotencyKey = "UNIQUE-IDEMPOTENCY-KEY-001",
                Items = new List<PosOrderItemDto>
                {
                    new PosOrderItemDto { ProductId = product.Id, Quantity = 1, UnitPrice = 86000 }
                },
                Payments = new List<PosPaymentDto>
                {
                    new PosPaymentDto { PaymentMethod = PaymentMethod.Cash, Amount = 86000 }
                }
            };

            // Act: Submit twice with same idempotency key
            var receipt1 = await posService.ProcessCheckoutAsync(checkoutDto, staff.Name);
            var receipt2 = await posService.ProcessCheckoutAsync(checkoutDto, staff.Name);

            // Assert: Trả về cùng một mã đơn hàng, không trừ kho 2 lần!
            Assert.Equal(receipt1.OrderNumber, receipt2.OrderNumber);

            var remainingStock = await inventoryService.GetProductStockInWarehouseAsync(warehouse.Id, product.Id);
            Assert.Equal(9, remainingStock); // Trừ 1 lần duy nhất: 10 - 1 = 9

            var ordersCount = await context.Orders.CountAsync();
            Assert.Equal(1, ordersCount);
        }

        [Fact]
        public async Task CloseShift_WhenDiscrepancyExceeds50kWithoutReason_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var dbName = "TestDb_Close_DiscFail_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);
            var (store, _, register, staff, _) = await SeedPosEnvironmentAsync(context);
            var shiftService = new ShiftService(context, NullLogger<ShiftService>.Instance);

            var shiftId = await shiftService.OpenShiftAsync(new ShiftOpenDto
            {
                StoreId = store.Id,
                RegisterId = register.Id,
                StaffId = staff.Id,
                OpeningCash = 1000000
            }, staff.Name);

            // Act & Assert: Expected = 1,000,000; Actual = 900,000 => Chênh lệch = -100,000đ (> 50,000đ) mà không có lý do
            var closeDto = new ShiftCloseDto
            {
                ShiftId = shiftId,
                ActualCashCounted = 900000,
                DiscrepancyReason = null // Không giải trình
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                shiftService.CloseShiftAsync(closeDto, staff.Name));

            Assert.Contains("50,000", ex.Message);
        }

        [Fact]
        public async Task CloseShift_WhenValid_ShouldMarkClosed_AndCalculateDiscrepancy()
        {
            // Arrange
            var dbName = "TestDb_Close_Success_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);
            var (store, _, register, staff, _) = await SeedPosEnvironmentAsync(context);
            var shiftService = new ShiftService(context, NullLogger<ShiftService>.Instance);

            var shiftId = await shiftService.OpenShiftAsync(new ShiftOpenDto
            {
                StoreId = store.Id,
                RegisterId = register.Id,
                StaffId = staff.Id,
                OpeningCash = 1000000
            }, staff.Name);

            // Act: Chênh lệch -80,000đ nhưng có giải trình hợp lệ
            var closeDto = new ShiftCloseDto
            {
                ShiftId = shiftId,
                ActualCashCounted = 920000,
                DiscrepancyReason = "Thu ngân thối nhầm tiền cho khách tại đơn ORD-01"
            };

            var closedShift = await shiftService.CloseShiftAsync(closeDto, staff.Name);

            // Assert
            Assert.Equal(ShiftStatus.Closed, closedShift.Status);
            Assert.Equal(-80000, closedShift.CashDiscrepancy);
            Assert.Equal(920000, closedShift.ActualCashCounted);
            Assert.Equal("Thu ngân thối nhầm tiền cho khách tại đơn ORD-01", closedShift.DiscrepancyReason);
            Assert.NotNull(closedShift.ClosedAt);
        }

        [Fact]
        public async Task GenerateShiftExcelReport_ShouldReturnValidXlsxBytes()
        {
            // Arrange
            var dbName = "TestDb_ExcelReport_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);
            var (store, warehouse, register, staff, product) = await SeedPosEnvironmentAsync(context, 10);

            var shiftService = new ShiftService(context, NullLogger<ShiftService>.Instance);
            var shiftId = await shiftService.OpenShiftAsync(new ShiftOpenDto
            {
                StoreId = store.Id,
                RegisterId = register.Id,
                StaffId = staff.Id,
                OpeningCash = 1000000
            }, staff.Name);

            await shiftService.CloseShiftAsync(new ShiftCloseDto
            {
                ShiftId = shiftId,
                ActualCashCounted = 1000000
            }, staff.Name);

            var inMemoryConfig = new ConfigurationBuilder().Build();
            var reportService = new ShiftReportService(context, inMemoryConfig, NullLogger<ShiftReportService>.Instance, new HttpClient());

            // Act
            var excelBytes = await reportService.GenerateShiftExcelReportAsync(shiftId);

            // Assert
            Assert.NotNull(excelBytes);
            Assert.True(excelBytes.Length > 1000, "File Excel phải có dung lượng thực tế > 1KB");
            // Kiểm tra magic header của file ZIP / XLSX: PK (0x50, 0x4B)
            Assert.Equal(0x50, excelBytes[0]);
            Assert.Equal(0x4B, excelBytes[1]);
        }
    }
}
