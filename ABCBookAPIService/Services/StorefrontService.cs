using ABCBook.Model;
using ABCBook.Model.Entity;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.DTOs.Storefront;
using ABCBookAPIService.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ABCBookAPIService.Services
{
    public class StorefrontService : IStorefrontService
    {
        private readonly ABCBookShopContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<StorefrontService> _logger;

        public StorefrontService(
            ABCBookShopContext context,
            IInventoryService inventoryService,
            ILogger<StorefrontService> logger)
        {
            _context = context;
            _inventoryService = inventoryService;
            _logger = logger;
        }

        public async Task<List<ProductDto>> GetFeaturedProductsAsync(int count = 8)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && !p.IsDeleted)
                .OrderByDescending(p => p.SellingPrice)
                .Take(count)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    SKU = p.SKU,
                    Barcode = p.Barcode,
                    Title = p.Title,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : "",
                    ProductType = p.ProductType,
                    Unit = p.Unit,
                    SellingPrice = p.SellingPrice,
                    CostPrice = p.CostPrice,
                    ImageUrl = p.ImageUrl,
                    IsActive = p.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<ProductDto>> GetNewArrivalsAsync(int count = 8)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    SKU = p.SKU,
                    Barcode = p.Barcode,
                    Title = p.Title,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : "",
                    ProductType = p.ProductType,
                    Unit = p.Unit,
                    SellingPrice = p.SellingPrice,
                    CostPrice = p.CostPrice,
                    ImageUrl = p.ImageUrl,
                    IsActive = p.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<CategoryDto>> GetActiveCategoriesAsync()
        {
            return await _context.Categories
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ProductsCount = c.Products.Count(p => p.IsActive && !p.IsDeleted)
                })
                .ToListAsync();
        }

        public async Task<ProductDto?> GetProductDetailsAsync(Guid id)
        {
            var p = await _context.Products
                .Include(x => x.Category)
                .Include(x => x.BookDetail)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (p == null) return null;

            return new ProductDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Barcode = p.Barcode,
                Title = p.Title,
                Description = p.Description,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? "Chưa phân loại",
                ProductType = p.ProductType,
                Unit = p.Unit,
                CostPrice = p.CostPrice,
                SellingPrice = p.SellingPrice,
                VatRate = p.VatRate,
                LowStockThreshold = p.LowStockThreshold,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                BookDetail = p.BookDetail != null ? new BookDetailDto
                {
                    ISBN = p.BookDetail.ISBN,
                    Author = p.BookDetail.Author,
                    Publisher = p.BookDetail.Publisher,
                    PublicationYear = p.BookDetail.PublicationYear,
                    PageCount = p.BookDetail.PageCount,
                    Language = p.BookDetail.Language,
                    CoverType = p.BookDetail.CoverType
                } : null
            };
        }

        public async Task<Dictionary<string, decimal>> GetStockAcrossStoresAsync(Guid productId)
        {
            var balances = await _context.StockBalances
                .Include(sb => sb.Warehouse)
                    .ThenInclude(w => w!.Store)
                .Where(sb => sb.ProductId == productId)
                .ToListAsync();

            var result = new Dictionary<string, decimal>();
            foreach (var b in balances)
            {
                var storeName = b.Warehouse?.Store?.Name ?? b.Warehouse?.Name ?? "Kho";
                if (!result.ContainsKey(storeName))
                {
                    result[storeName] = b.Quantity;
                }
                else
                {
                    result[storeName] += b.Quantity;
                }
            }

            return result;
        }

        public async Task<Guid> PlaceOnlineOrderAsync(ShoppingCartDto cart, PublicCheckoutDto checkoutInfo)
        {
            if (cart == null || !cart.Items.Any())
            {
                throw new InvalidOperationException("Giỏ hàng của bạn đang trống.");
            }

            // 1. Tìm hoặc tạo khách hàng trực tuyến
            var cleanPhone = checkoutInfo.PhoneNumber.Trim();
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.PhoneNumber == cleanPhone && !c.IsDeleted);

            if (customer == null)
            {
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    CustomerCode = $"KH-ONL-{DateTime.UtcNow:yyMM}{RandomNumberGenerator.GetInt32(1000, 9999)}",
                    FullName = checkoutInfo.FullName.Trim(),
                    PhoneNumber = cleanPhone,
                    Email = checkoutInfo.Email?.Trim(),
                    Address = checkoutInfo.ShippingAddress.Trim(),
                    RewardPoints = 0,
                    MembershipTier = "Standard",
                    IsActive = true,
                    CreatedBy = "OnlineCustomer",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Customers.AddAsync(customer);
            }

            // 2. Chọn Kho xuất hàng online (Ưu tiên kho Tổng Central hoặc Kho Store đầu tiên)
            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.IsCentralWarehouse && w.IsActive && !w.IsDeleted)
                            ?? await _context.Warehouses.FirstOrDefaultAsync(w => w.IsActive && !w.IsDeleted)
                            ?? throw new InvalidOperationException("Không tìm thấy kho xuất hàng trực tuyến.");

            var storeId = warehouse.StoreId;
            var orderNumber = $"WEB-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(10000, 99999)}";

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                IdempotencyKey = Guid.NewGuid().ToString(),
                Source = OrderSource.WebEcommerce,
                Status = checkoutInfo.PaymentMethod == PaymentMethod.Cod ? OrderStatus.Completed : OrderStatus.PendingPayment,
                StoreId = storeId,
                CustomerId = customer.Id,
                SubTotal = cart.SubTotal,
                DiscountAmount = cart.DiscountAmount,
                VatAmount = 0,
                TotalAmount = cart.TotalAmount,
                PaidAmount = checkoutInfo.PaymentMethod == PaymentMethod.Cod ? 0 : cart.TotalAmount,
                ChangeAmount = 0,
                Note = $"[Địa chỉ giao: {checkoutInfo.ShippingAddress}] - {checkoutInfo.Note}".Trim(),
                CreatedBy = "E-Commerce",
                CreatedAt = DateTime.UtcNow
            };

            // 3. Thêm chi tiết đơn hàng & Trừ tồn kho qua Sổ cái kép
            foreach (var item in cart.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId)
                    ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm {item.ProductId}");

                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Title,
                    SKU = item.SKU,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    CostPrice = product.CostPrice,
                    DiscountAmount = 0
                });

                // Trừ tồn kho có bảo vệ chống âm và ghi nhận sổ cái giao dịch
                await _inventoryService.DeductStockAsync(
                    warehouse.Id,
                    item.ProductId,
                    item.Quantity,
                    orderNumber,
                    StockTransactionType.OnlineSale,
                    $"Xuất bán online đơn hàng {orderNumber}",
                    "E-Commerce Engine");
            }

            // 4. Ghi nhận giao dịch thanh toán
            order.Payments.Add(new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentMethod = checkoutInfo.PaymentMethod,
                Amount = cart.TotalAmount,
                TransactionReference = checkoutInfo.PaymentMethod == PaymentMethod.VietQR ? $"VQR-{orderNumber}" : "COD",
                IsSuccessful = checkoutInfo.PaymentMethod != PaymentMethod.VietQR,
                PaidAt = DateTime.UtcNow
            });

            // 5. Tích điểm thưởng khách hàng
            int pointsEarned = (int)(cart.TotalAmount / 10000);
            if (pointsEarned > 0)
            {
                customer.RewardPoints += pointsEarned;
                customer.TotalSpending += cart.TotalAmount;
                await _context.LoyaltyPointLedgers.AddAsync(new LoyaltyPointLedger
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    OrderId = order.Id,
                    TransactionType = LoyaltyTransactionType.EarnFromOrder,
                    PointsChanged = pointsEarned,
                    BalanceAfter = customer.RewardPoints,
                    Reason = $"Tích {pointsEarned} điểm từ đơn hàng trực tuyến {orderNumber}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Khách hàng đã đặt đơn online {OrderNumber} thành công: {TotalAmount:N0} ₫ qua {Method}",
                orderNumber, cart.TotalAmount, checkoutInfo.PaymentMethod);

            return order.Id;
        }

        public async Task<Order?> TrackOrderAsync(string orderNumber, string phoneNumber)
        {
            var cleanNo = (orderNumber ?? "").Trim();
            var cleanPhone = (phoneNumber ?? "").Trim();

            return await _context.Orders
                .Include(o => o.Store)
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderNumber == cleanNo && o.Customer != null && o.Customer.PhoneNumber == cleanPhone && !o.IsDeleted);
        }

        public async Task<ChainDashboardSummaryDto> GetChainDashboardSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // 1. Doanh thu hôm nay
            var todayOrders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= todayStart && !o.IsDeleted)
                .ToListAsync();

            decimal todayRevenue = todayOrders.Sum(o => o.TotalAmount);
            int todayCount = todayOrders.Count;

            // 2. Doanh thu tháng này
            var monthOrders = await _context.Orders
                .Include(o => o.Store)
                .Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= monthStart && !o.IsDeleted)
                .ToListAsync();

            decimal monthRevenue = monthOrders.Sum(o => o.TotalAmount);
            int monthCount = monthOrders.Count;

            // 3. Cảnh báo tồn kho thấp & Giá trị tồn kho toàn chuỗi
            var balances = await _context.StockBalances
                .Include(sb => sb.Product)
                .ToListAsync();

            int lowStockCount = balances.Count(sb => sb.Product != null && sb.Quantity <= sb.Product.LowStockThreshold);
            decimal totalInvValue = balances.Sum(sb => sb.Quantity * (sb.Product?.CostPrice ?? 0));

            // 4. Hiệu quả kinh doanh theo từng Cửa hàng (Store Performance)
            var storePerformances = monthOrders
                .GroupBy(o => o.StoreId)
                .Select(g =>
                {
                    var store = g.First().Store;
                    var storeRev = g.Sum(o => o.TotalAmount);
                    return new StorePerformanceDto
                    {
                        StoreId = g.Key,
                        StoreCode = store?.Code ?? "",
                        StoreName = store?.Name ?? "Cửa hàng",
                        Revenue = storeRev,
                        OrdersCount = g.Count(),
                        PercentageOfTotal = monthRevenue > 0 ? Math.Round((storeRev / monthRevenue) * 100, 1) : 0
                    };
                })
                .OrderByDescending(s => s.Revenue)
                .ToList();

            // 5. Top 10 sản phẩm bán chạy nhất trong tháng
            var completedItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p!.Category)
                .Where(oi => oi.Order != null && oi.Order.Status == OrderStatus.Completed
                             && oi.Order.CreatedAt >= monthStart && !oi.Order.IsDeleted)
                .ToListAsync();

            var topProducts = completedItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new TopSellingProductDto
                {
                    ProductId = g.Key,
                    SKU = g.First().SKU,
                    Title = g.First().ProductName,
                    CategoryName = g.First().Product != null && g.First().Product!.Category != null ? g.First().Product!.Category!.Name : "Sách",
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.LineTotal)
                })
                .OrderByDescending(p => p.QuantitySold)
                .Take(10)
                .ToList();

            // 6. 10 đơn hàng gần nhất
            var recentOrders = await _context.Orders
                .Include(o => o.Store)
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new RecentOrderDto
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    StoreName = o.Store != null ? o.Store.Name : "Hệ thống",
                    CustomerName = o.Customer != null ? o.Customer.FullName : "Khách lẻ tại quầy",
                    Source = o.Source,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return new ChainDashboardSummaryDto
            {
                TodayRevenue = todayRevenue,
                TodayOrdersCount = todayCount,
                MonthRevenue = monthRevenue,
                MonthOrdersCount = monthCount,
                LowStockAlertCount = lowStockCount,
                TotalInventoryValue = totalInvValue,
                StorePerformances = storePerformances,
                TopSellingProducts = topProducts,
                RecentOrders = recentOrders
            };
        }
    }
}
