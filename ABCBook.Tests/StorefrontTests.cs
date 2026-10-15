using ABCBook.Model;
using ABCBook.Model.Entity;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Storefront;
using ABCBookAPIService.Interfaces;
using ABCBookAPIService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ABCBook.Tests
{
    public class StorefrontTests
    {
        private ABCBookShopContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ABCBookShopContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ABCBookShopContext(options);
        }

        [Fact]
        public void ShoppingCart_Calculations_ShouldProperlyComputeSubTotalShippingAndTotal()
        {
            // Case 1: SubTotal < 250,000đ -> Shipping fee applies (e.g. 30,000đ)
            var cart1 = new ShoppingCartDto
            {
                ShippingFee = 30000,
                Items = new List<CartItemDto>
                {
                    new CartItemDto { ProductId = Guid.NewGuid(), Title = "Đắc Nhân Tâm", UnitPrice = 85000, Quantity = 2 }
                }
            };

            Assert.Equal(170000, cart1.SubTotal);
            Assert.Equal(2, cart1.TotalItemCount);
            Assert.Equal(200000, cart1.TotalAmount); // 170k + 30k ship

            // Case 2: SubTotal >= 250,000đ -> Free shipping
            var cart2 = new ShoppingCartDto
            {
                ShippingFee = 0,
                DiscountAmount = 20000,
                Items = new List<CartItemDto>
                {
                    new CartItemDto { ProductId = Guid.NewGuid(), Title = "Nhà Giả Kim", UnitPrice = 79000, Quantity = 2 },
                    new CartItemDto { ProductId = Guid.NewGuid(), Title = "Tuổi Trẻ Đáng Giá Bao Nhiêu", UnitPrice = 85000, Quantity = 2 }
                }
            };

            Assert.Equal(328000, cart2.SubTotal);
            Assert.Equal(4, cart2.TotalItemCount);
            Assert.Equal(308000, cart2.TotalAmount); // 328k - 20k discount + 0k ship
        }

        [Fact]
        public async Task PlaceOnlineOrder_WithCod_ShouldCreateOrderDeductCentralStockAndAccrueLoyaltyPoints()
        {
            // Arrange
            var dbName = "TestDb_OnlineOrder_Cod_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);

            var store = new Store { Id = Guid.NewGuid(), Code = "STORE-01", Name = "Nhà sách Nguyễn Huệ", Address = "Q1", Phone = "0901", IsActive = true };
            await context.Stores.AddAsync(store);

            var centralWarehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                StoreId = store.Id,
                Code = "WH-CENTRAL",
                Name = "Kho Tổng Trung Tâm",
                IsCentralWarehouse = true,
                IsActive = true
            };
            await context.Warehouses.AddAsync(centralWarehouse);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                SKU = "BK-DNT-01",
                Barcode = "893000000001",
                Title = "Đắc Nhân Tâm",
                SellingPrice = 120000,
                CostPrice = 70000,
                IsActive = true
            };
            await context.Products.AddAsync(product);

            var stockBalance = new StockBalance
            {
                WarehouseId = centralWarehouse.Id,
                ProductId = product.Id,
                Quantity = 25
            };
            await context.StockBalances.AddAsync(stockBalance);
            await context.SaveChangesAsync();

            var invService = new InventoryService(context);
            var storefrontService = new StorefrontService(context, invService, NullLogger<StorefrontService>.Instance);

            var cart = new ShoppingCartDto
            {
                Items = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = product.Id,
                        SKU = product.SKU,
                        Title = product.Title,
                        UnitPrice = product.SellingPrice,
                        Quantity = 3
                    }
                }
            };

            var checkoutInfo = new PublicCheckoutDto
            {
                FullName = "Nguyễn Văn A",
                PhoneNumber = "0912345678",
                Email = "nguyenvana@gmail.com",
                ShippingAddress = "789 Điện Biên Phủ, Q3, TP.HCM",
                PaymentMethod = PaymentMethod.Cod,
                Note = "Giao giờ hành chính"
            };

            // Act
            var orderId = await storefrontService.PlaceOnlineOrderAsync(cart, checkoutInfo);

            // Assert
            var savedOrder = await context.Orders
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            Assert.NotNull(savedOrder);
            Assert.Equal(OrderStatus.Completed, savedOrder.Status);
            Assert.Equal(OrderSource.WebEcommerce, savedOrder.Source);
            Assert.Equal(360000, savedOrder.TotalAmount);
            Assert.Single(savedOrder.Items);
            Assert.Equal(3, savedOrder.Items.First().Quantity);

            // Verify Double-Entry Stock Deduction
            var updatedStock = await context.StockBalances
                .FirstOrDefaultAsync(sb => sb.WarehouseId == centralWarehouse.Id && sb.ProductId == product.Id);
            Assert.NotNull(updatedStock);
            Assert.Equal(22, updatedStock.Quantity); // 25 - 3 = 22

            var stockTx = await context.StockTransactions
                .FirstOrDefaultAsync(t => t.ProductId == product.Id && t.TransactionType == StockTransactionType.OnlineSale);
            Assert.NotNull(stockTx);
            Assert.Equal(-3, stockTx.Quantity);
            Assert.Equal(22, stockTx.AfterStock);

            // Verify Customer & Points Accrual (360k / 10k = 36 points)
            Assert.NotNull(savedOrder.Customer);
            Assert.Equal("Nguyễn Văn A", savedOrder.Customer.FullName);
            Assert.Equal("0912345678", savedOrder.Customer.PhoneNumber);
            Assert.Equal(36, savedOrder.Customer.RewardPoints);

            var pointLedger = await context.LoyaltyPointLedgers
                .FirstOrDefaultAsync(l => l.CustomerId == savedOrder.CustomerId && l.OrderId == savedOrder.Id);
            Assert.NotNull(pointLedger);
            Assert.Equal(36, pointLedger.PointsChanged);
        }

        [Fact]
        public async Task PlaceOnlineOrder_WithVietQR_ShouldSetPendingPaymentAndReference()
        {
            // Arrange
            var dbName = "TestDb_OnlineOrder_VQR_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);

            var store = new Store { Id = Guid.NewGuid(), Code = "STORE-01", Name = "Nhà sách Nguyễn Huệ", Address = "Q1", Phone = "0901", IsActive = true };
            await context.Stores.AddAsync(store);

            var centralWarehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                StoreId = store.Id,
                Code = "WH-CENTRAL",
                Name = "Kho Tổng Trung Tâm",
                IsCentralWarehouse = true,
                IsActive = true
            };
            await context.Warehouses.AddAsync(centralWarehouse);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                SKU = "BK-KNS-01",
                Barcode = "893000000002",
                Title = "Tuổi Trẻ Không Trì Hoãn",
                SellingPrice = 90000,
                CostPrice = 50000,
                IsActive = true
            };
            await context.Products.AddAsync(product);

            var stockBalance = new StockBalance
            {
                WarehouseId = centralWarehouse.Id,
                ProductId = product.Id,
                Quantity = 10
            };
            await context.StockBalances.AddAsync(stockBalance);
            await context.SaveChangesAsync();

            var invService = new InventoryService(context);
            var storefrontService = new StorefrontService(context, invService, NullLogger<StorefrontService>.Instance);

            var cart = new ShoppingCartDto
            {
                Items = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = product.Id,
                        SKU = product.SKU,
                        Title = product.Title,
                        UnitPrice = product.SellingPrice,
                        Quantity = 1
                    }
                }
            };

            var checkoutInfo = new PublicCheckoutDto
            {
                FullName = "Trần Thị B",
                PhoneNumber = "0987654321",
                ShippingAddress = "12 Hai Bà Trưng, Q1, TP.HCM",
                PaymentMethod = PaymentMethod.VietQR
            };

            // Act
            var orderId = await storefrontService.PlaceOnlineOrderAsync(cart, checkoutInfo);

            // Assert
            var savedOrder = await context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            Assert.NotNull(savedOrder);
            Assert.Equal(OrderStatus.PendingPayment, savedOrder.Status);
            Assert.Single(savedOrder.Payments);

            var payment = savedOrder.Payments.First();
            Assert.Equal(PaymentMethod.VietQR, payment.PaymentMethod);
            Assert.False(payment.IsSuccessful);
            Assert.StartsWith($"VQR-{savedOrder.OrderNumber}", payment.TransactionReference);
        }

        [Fact]
        public async Task PlaceOnlineOrder_InsufficientStock_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var dbName = "TestDb_OnlineOrder_InsufficientStock_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);

            var store = new Store { Id = Guid.NewGuid(), Code = "STORE-01", Name = "Nhà sách ABC", Address = "HCM", Phone = "090", IsActive = true };
            await context.Stores.AddAsync(store);

            var centralWarehouse = new Warehouse { Id = Guid.NewGuid(), StoreId = store.Id, Code = "WH-CENTRAL", Name = "Kho Tổng", IsCentralWarehouse = true, IsActive = true };
            await context.Warehouses.AddAsync(centralWarehouse);

            var product = new Product { Id = Guid.NewGuid(), SKU = "BK-LTD-01", Barcode = "893000000003", Title = "Lập Trình .NET 8", SellingPrice = 250000, CostPrice = 150000, IsActive = true };
            await context.Products.AddAsync(product);

            // Stock is only 2
            var stockBalance = new StockBalance { WarehouseId = centralWarehouse.Id, ProductId = product.Id, Quantity = 2 };
            await context.StockBalances.AddAsync(stockBalance);
            await context.SaveChangesAsync();

            var invService = new InventoryService(context);
            var storefrontService = new StorefrontService(context, invService, NullLogger<StorefrontService>.Instance);

            // Cart requests 5 (more than 2)
            var cart = new ShoppingCartDto
            {
                Items = new List<CartItemDto>
                {
                    new CartItemDto { ProductId = product.Id, SKU = product.SKU, Title = product.Title, UnitPrice = product.SellingPrice, Quantity = 5 }
                }
            };

            var checkoutInfo = new PublicCheckoutDto
            {
                FullName = "Lê Hoàng C",
                PhoneNumber = "0933334444",
                ShippingAddress = "456 CMT8, Q10",
                PaymentMethod = PaymentMethod.Cod
            };

            // Act & Assert
            await Assert.ThrowsAsync<InsufficientStockException>(() =>
                storefrontService.PlaceOnlineOrderAsync(cart, checkoutInfo));
        }

        [Fact]
        public async Task TrackOrder_WithValidAndInvalidPhone_ShouldReturnExpectedResult()
        {
            // Arrange
            var dbName = "TestDb_TrackOrder_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                CustomerCode = "KH-001",
                FullName = "Đỗ Quốc D",
                PhoneNumber = "0977889900",
                IsActive = true
            };
            await context.Customers.AddAsync(customer);

            var store = new Store { Id = Guid.NewGuid(), Code = "STORE-01", Name = "Nhà sách ABC", Address = "HCM", Phone = "090", IsActive = true };
            await context.Stores.AddAsync(store);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "WEB-20261015-99999",
                StoreId = store.Id,
                CustomerId = customer.Id,
                SubTotal = 150000,
                TotalAmount = 150000,
                Status = OrderStatus.Completed,
                Source = OrderSource.WebEcommerce,
                CreatedAt = DateTime.UtcNow
            };
            await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();

            var invService = new InventoryService(context);
            var storefrontService = new StorefrontService(context, invService, NullLogger<StorefrontService>.Instance);

            // Act: Correct Phone
            var resultValid = await storefrontService.TrackOrderAsync("WEB-20261015-99999", "0977889900");

            // Act: Wrong Phone
            var resultInvalid = await storefrontService.TrackOrderAsync("WEB-20261015-99999", "0900000000");

            // Assert
            Assert.NotNull(resultValid);
            Assert.Equal(order.Id, resultValid.Id);
            Assert.Equal("WEB-20261015-99999", resultValid.OrderNumber);
            Assert.Equal("Đỗ Quốc D", resultValid.Customer?.FullName);

            Assert.Null(resultInvalid);
        }

        [Fact]
        public async Task GetChainDashboardSummary_ShouldAccuratelyCalculateMetricsAcrossStores()
        {
            // Arrange
            var dbName = "TestDb_Dashboard_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);

            var store1 = new Store { Id = Guid.NewGuid(), Code = "STORE-Q1", Name = "Nhà sách Quận 1", Address = "Q1", Phone = "091", IsActive = true };
            var store2 = new Store { Id = Guid.NewGuid(), Code = "STORE-BT", Name = "Nhà sách Bình Thạnh", Address = "BT", Phone = "092", IsActive = true };
            await context.Stores.AddRangeAsync(store1, store2);

            var wh1 = new Warehouse { Id = Guid.NewGuid(), StoreId = store1.Id, Code = "WH-Q1", Name = "Kho Q1", IsActive = true };
            var wh2 = new Warehouse { Id = Guid.NewGuid(), StoreId = store2.Id, Code = "WH-BT", Name = "Kho BT", IsActive = true };
            await context.Warehouses.AddRangeAsync(wh1, wh2);

            var product1 = new Product { Id = Guid.NewGuid(), SKU = "BK-01", Barcode = "001", Title = "Kinh Tế Học Cơ Bản", CostPrice = 80000, SellingPrice = 120000, LowStockThreshold = 10, IsActive = true };
            var product2 = new Product { Id = Guid.NewGuid(), SKU = "BK-02", Barcode = "002", Title = "Tư Duy Nhanh Và Chậm", CostPrice = 100000, SellingPrice = 180000, LowStockThreshold = 5, IsActive = true };
            await context.Products.AddRangeAsync(product1, product2);

            // Stock balances: product1 has 4 in wh1 (<= threshold 10 -> low stock alert)
            // product2 has 20 in wh2 (> threshold 5)
            var sb1 = new StockBalance { WarehouseId = wh1.Id, ProductId = product1.Id, Quantity = 4 };
            var sb2 = new StockBalance { WarehouseId = wh2.Id, ProductId = product2.Id, Quantity = 20 };
            await context.StockBalances.AddRangeAsync(sb1, sb2);

            // Completed orders today
            var order1 = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "POS-001",
                StoreId = store1.Id,
                SubTotal = 240000,
                TotalAmount = 240000,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = Guid.NewGuid(), ProductId = product1.Id, SKU = product1.SKU, ProductName = product1.Title, Quantity = 2, UnitPrice = 120000 }
                }
            };

            var order2 = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "POS-002",
                StoreId = store2.Id,
                SubTotal = 360000,
                TotalAmount = 360000,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = Guid.NewGuid(), ProductId = product2.Id, SKU = product2.SKU, ProductName = product2.Title, Quantity = 2, UnitPrice = 180000 }
                }
            };

            await context.Orders.AddRangeAsync(order1, order2);
            await context.SaveChangesAsync();

            var invService = new InventoryService(context);
            var storefrontService = new StorefrontService(context, invService, NullLogger<StorefrontService>.Instance);

            // Act
            var summary = await storefrontService.GetChainDashboardSummaryAsync();

            // Assert
            Assert.NotNull(summary);
            Assert.Equal(600000, summary.TodayRevenue); // 240k + 360k
            Assert.Equal(2, summary.TodayOrdersCount);
            Assert.Equal(600000, summary.MonthRevenue);
            Assert.Equal(2, summary.MonthOrdersCount);
            Assert.Equal(1, summary.LowStockAlertCount); // sb1 has 4 <= 10
            Assert.Equal((4 * 80000) + (20 * 100000), summary.TotalInventoryValue); // 320k + 2000k = 2320k

            // Store performances
            Assert.Equal(2, summary.StorePerformances.Count);
            var topStore = summary.StorePerformances.First();
            Assert.Equal(store2.Id, topStore.StoreId); // 360k > 240k
            Assert.Equal(360000, topStore.Revenue);
            Assert.Equal(60.0m, topStore.PercentageOfTotal);

            // Top selling products
            Assert.Equal(2, summary.TopSellingProducts.Count);
            Assert.Contains(summary.TopSellingProducts, p => p.ProductId == product1.Id && p.QuantitySold == 2);
            Assert.Contains(summary.TopSellingProducts, p => p.ProductId == product2.Id && p.QuantitySold == 2);
        }
    }
}
