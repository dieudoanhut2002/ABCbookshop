using ABCBook.Model;
using ABCBook.Model.Entity;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.POS;
using ABCBookAPIService.Interfaces;
using ClosedXML.Excel;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ABCBookAPIService.Services
{
    public class ShiftService : IShiftService
    {
        private readonly ABCBookShopContext _context;
        private readonly ILogger<ShiftService> _logger;

        public ShiftService(ABCBookShopContext context, ILogger<ShiftService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ShiftDetailDto?> GetActiveShiftForRegisterAsync(Guid registerId)
        {
            var shift = await _context.ShiftSessions
                .Include(s => s.Store)
                .Include(s => s.Register)
                .Include(s => s.Staff)
                .FirstOrDefaultAsync(s => s.RegisterId == registerId && s.Status == ShiftStatus.Open && !s.IsDeleted);

            return shift != null ? MapToDetailDto(shift) : null;
        }

        public async Task<ShiftDetailDto?> GetActiveShiftForStaffAsync(Guid staffId)
        {
            var shift = await _context.ShiftSessions
                .Include(s => s.Store)
                .Include(s => s.Register)
                .Include(s => s.Staff)
                .FirstOrDefaultAsync(s => s.StaffId == staffId && s.Status == ShiftStatus.Open && !s.IsDeleted);

            return shift != null ? MapToDetailDto(shift) : null;
        }

        public async Task<ShiftDetailDto?> GetShiftByIdAsync(Guid shiftId)
        {
            var shift = await _context.ShiftSessions
                .Include(s => s.Store)
                .Include(s => s.Register)
                .Include(s => s.Staff)
                .FirstOrDefaultAsync(s => s.Id == shiftId && !s.IsDeleted);

            return shift != null ? MapToDetailDto(shift) : null;
        }

        public async Task<Guid> OpenShiftAsync(ShiftOpenDto dto, string operatorName)
        {
            var existingOpenShift = await _context.ShiftSessions
                .FirstOrDefaultAsync(s => s.RegisterId == dto.RegisterId && s.Status == ShiftStatus.Open && !s.IsDeleted);

            if (existingOpenShift != null)
            {
                throw new InvalidOperationException($"Quầy thu ngân này đang có ca làm việc chưa đóng (Mã ca: {existingOpenShift.ShiftCode}). Vui lòng đóng ca trước khi mở ca mới.");
            }

            var store = await _context.Stores.FindAsync(dto.StoreId)
                ?? throw new KeyNotFoundException("Không tìm thấy Cửa hàng.");

            var register = await _context.POSRegisters.FindAsync(dto.RegisterId)
                ?? throw new KeyNotFoundException("Không tìm thấy Quầy POS.");

            // Tìm nhân viên mở ca (nếu chưa truyền StaffId, tìm theo operatorName hoặc staff đầu tiên)
            Guid staffId = dto.StaffId ?? Guid.Empty;
            if (staffId == Guid.Empty)
            {
                var staff = await _context.Staffs.FirstOrDefaultAsync(s => s.StoreID == dto.StoreId && !s.IsDeleted);
                if (staff != null) staffId = staff.Id;
            }

            var shiftCode = $"SFT-{store.Code}-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 9999)}";

            var shift = new ShiftSession
            {
                Id = Guid.NewGuid(),
                ShiftCode = shiftCode,
                StoreId = dto.StoreId,
                RegisterId = dto.RegisterId,
                StaffId = staffId,
                Status = ShiftStatus.Open,
                OpenedAt = DateTime.UtcNow,
                OpeningCash = dto.OpeningCash,
                ExpectedCash = dto.OpeningCash,
                CreatedBy = operatorName,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ShiftSessions.AddAsync(shift);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Đã mở ca POS thành công: {ShiftCode} tại quầy {RegisterCode} bởi {Operator}",
                shift.ShiftCode, register.RegisterCode, operatorName);

            return shift.Id;
        }

        public async Task<ShiftDetailDto> CloseShiftAsync(ShiftCloseDto dto, string operatorName)
        {
            var shift = await _context.ShiftSessions
                .Include(s => s.Store)
                .Include(s => s.Register)
                .Include(s => s.Staff)
                .FirstOrDefaultAsync(s => s.Id == dto.ShiftId && !s.IsDeleted)
                ?? throw new KeyNotFoundException("Không tìm thấy Ca làm việc.");

            if (shift.Status != ShiftStatus.Open)
            {
                throw new InvalidOperationException("Ca làm việc này đã được đóng hoặc duyệt trước đó, không thể đóng lại.");
            }

            // Tính chênh lệch tiền mặt thực tế vs dự kiến
            decimal diff = dto.ActualCashCounted - shift.ExpectedCash;

            // Ngưỡng chênh lệch bắt buộc giải trình: 50,000 VND
            if (Math.Abs(diff) > 50000 && string.IsNullOrWhiteSpace(dto.DiscrepancyReason))
            {
                throw new InvalidOperationException($"Chênh lệch tiền mặt ({diff:N0} ₫) vượt quá ngưỡng quy định (50,000 ₫). Thu ngân bắt buộc phải nhập lý do giải trình kiểm đếm.");
            }

            shift.Status = ShiftStatus.Closed;
            shift.ClosedAt = DateTime.UtcNow;
            shift.ActualCashCounted = dto.ActualCashCounted;
            shift.CashDiscrepancy = diff;
            shift.DiscrepancyReason = dto.DiscrepancyReason?.Trim() ?? string.Empty;
            shift.UpdatedBy = operatorName;
            shift.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Đã đóng ca POS: {ShiftCode}. Thực kiểm: {Actual:N0} ₫, Dự kiến: {Expected:N0} ₫, Chênh lệch: {Diff:N0} ₫",
                shift.ShiftCode, dto.ActualCashCounted, shift.ExpectedCash, diff);

            return MapToDetailDto(shift);
        }

        public async Task<List<ShiftDetailDto>> GetRecentShiftsAsync(Guid? storeId = null, int count = 20)
        {
            var query = _context.ShiftSessions
                .Include(s => s.Store)
                .Include(s => s.Register)
                .Include(s => s.Staff)
                .Where(s => !s.IsDeleted);

            if (storeId.HasValue) query = query.Where(s => s.StoreId == storeId.Value);

            var shifts = await query
                .OrderByDescending(s => s.OpenedAt)
                .Take(count)
                .ToListAsync();

            return shifts.Select(MapToDetailDto).ToList();
        }

        public async Task<bool> ApproveShiftAsync(Guid shiftId, string operatorName)
        {
            var shift = await _context.ShiftSessions.FindAsync(shiftId);
            if (shift == null || shift.Status != ShiftStatus.Closed) return false;

            shift.Status = ShiftStatus.Approved;
            shift.IsDiscrepancyApproved = true;
            shift.UpdatedBy = operatorName;
            shift.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private static ShiftDetailDto MapToDetailDto(ShiftSession s)
        {
            return new ShiftDetailDto
            {
                Id = s.Id,
                ShiftCode = s.ShiftCode,
                StoreId = s.StoreId,
                StoreName = s.Store?.Name ?? "",
                RegisterId = s.RegisterId,
                RegisterCode = s.Register?.RegisterCode ?? "",
                RegisterName = s.Register?.Name ?? "",
                StaffId = s.StaffId,
                StaffName = s.Staff?.Name ?? "Thu ngân",
                Status = s.Status,
                OpenedAt = s.OpenedAt,
                ClosedAt = s.ClosedAt,
                OpeningCash = s.OpeningCash,
                TotalCashSales = s.TotalCashSales,
                TotalVietQrSales = s.TotalVietQrSales,
                TotalCardSales = s.TotalCardSales,
                TotalCashPaidOut = s.TotalCashPaidOut,
                ExpectedCash = s.ExpectedCash,
                ActualCashCounted = s.ActualCashCounted,
                CashDiscrepancy = s.CashDiscrepancy,
                DiscrepancyReason = s.DiscrepancyReason,
                CompletedOrdersCount = s.CompletedOrdersCount
            };
        }
    }

    public class PosService : IPosService
    {
        private readonly ABCBookShopContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<PosService> _logger;

        public PosService(ABCBookShopContext context, IInventoryService inventoryService, ILogger<PosService> logger)
        {
            _context = context;
            _inventoryService = inventoryService;
            _logger = logger;
        }

        public async Task<List<PosProductSearchDto>> SearchProductsForPosAsync(Guid warehouseId, string keyword)
        {
            var term = (keyword ?? "").Trim().ToLower();

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.StockBalances.Where(sb => sb.WarehouseId == warehouseId))
                .Where(p => p.IsActive && !p.IsDeleted);

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(p =>
                    p.Barcode.ToLower() == term ||
                    p.SKU.ToLower().Contains(term) ||
                    p.Title.ToLower().Contains(term));
            }

            var products = await query
                .OrderBy(p => p.Title)
                .Take(25)
                .ToListAsync();

            return products.Select(p =>
            {
                var sb = p.StockBalances.FirstOrDefault(b => b.WarehouseId == warehouseId);
                return new PosProductSearchDto
                {
                    Id = p.Id,
                    SKU = p.SKU,
                    Barcode = p.Barcode,
                    Title = p.Title,
                    SellingPrice = p.SellingPrice,
                    Unit = p.Unit,
                    AvailableStock = sb?.Quantity ?? 0,
                    CategoryName = p.Category?.Name ?? "Chưa phân loại",
                    ImageUrl = p.ImageUrl
                };
            }).ToList();
        }

        public async Task<Customer?> FindCustomerByPhoneAsync(string phone)
        {
            var cleanPhone = phone.Trim();
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.PhoneNumber == cleanPhone && !c.IsDeleted);
        }

        public async Task<Customer> CreateQuickCustomerAsync(string fullName, string phone, string operatorName)
        {
            var cleanPhone = phone.Trim();
            var existing = await FindCustomerByPhoneAsync(cleanPhone);
            if (existing != null) return existing;

            var code = $"KH-{DateTime.UtcNow:yyMM}{RandomNumberGenerator.GetInt32(1000, 9999)}";
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                CustomerCode = code,
                FullName = fullName.Trim(),
                PhoneNumber = cleanPhone,
                RewardPoints = 0,
                MembershipTier = "Standard",
                IsActive = true,
                CreatedBy = operatorName,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<PosOrderReceiptDto> ProcessCheckoutAsync(PosCheckoutDto dto, string operatorName)
        {
            // 1. Kiểm tra chống trùng đơn thanh toán (Idempotency Key)
            if (!string.IsNullOrEmpty(dto.IdempotencyKey))
            {
                var existingOrder = await _context.Orders
                    .Include(o => o.Store)
                    .Include(o => o.Customer)
                    .Include(o => o.Items)
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.IdempotencyKey == dto.IdempotencyKey);

                if (existingOrder != null)
                {
                    _logger.LogWarning("Phát hiện yêu cầu thanh toán trùng lặp (IdempotencyKey: {Key}). Trả về hóa đơn đã tạo.", dto.IdempotencyKey);
                    return MapToReceipt(existingOrder);
                }
            }

            // 2. Kiểm tra ca làm việc
            var shift = await _context.ShiftSessions
                .Include(s => s.Store)
                .Include(s => s.Register)
                .Include(s => s.Staff)
                .FirstOrDefaultAsync(s => s.Id == dto.ShiftSessionId && !s.IsDeleted)
                ?? throw new KeyNotFoundException("Không tìm thấy Ca làm việc của quầy thu ngân.");

            if (shift.Status != ShiftStatus.Open)
            {
                throw new InvalidOperationException("Ca làm việc đã bị đóng. Không thể tạo đơn bán hàng.");
            }

            if (dto.Items == null || !dto.Items.Any())
            {
                throw new ArgumentException("Giỏ hàng rỗng. Vui lòng thêm ít nhất một sản phẩm.");
            }

            var store = await _context.Stores.FindAsync(dto.StoreId)
                ?? throw new KeyNotFoundException("Không tìm thấy Cửa hàng.");

            // 3. Tính toán tiền hàng & Giảm giá
            decimal subTotal = 0;
            var orderItems = new List<OrderItem>();
            var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(10000, 99999)}";

            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            foreach (var itemDto in dto.Items)
            {
                if (!products.TryGetValue(itemDto.ProductId, out var product))
                {
                    throw new KeyNotFoundException($"Không tìm thấy sản phẩm có ID: {itemDto.ProductId}");
                }

                decimal lineSubTotal = itemDto.Quantity * itemDto.UnitPrice;
                decimal lineTotal = lineSubTotal - itemDto.DiscountAmount;
                subTotal += lineTotal;

                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = itemDto.ProductId,
                    ProductName = product.Title,
                    SKU = product.SKU,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    CostPrice = product.CostPrice,
                    DiscountAmount = itemDto.DiscountAmount
                });

                // 4. Trừ tồn kho qua Sổ cái kép (Bảo vệ chống âm kho và lưu vết)
                await _inventoryService.DeductStockAsync(
                    dto.WarehouseId,
                    itemDto.ProductId,
                    itemDto.Quantity,
                    orderNumber,
                    StockTransactionType.PosSale,
                    $"Bán lẻ quầy POS theo đơn {orderNumber}",
                    operatorName);
            }

            // Xử lý đổi điểm thưởng tích lũy (1 điểm = 1,000 VND)
            Customer? customer = null;
            decimal loyaltyDiscount = 0;
            if (dto.CustomerId.HasValue)
            {
                customer = await _context.Customers.FindAsync(dto.CustomerId.Value);
                if (customer != null && dto.PointsToRedeem > 0)
                {
                    if (customer.RewardPoints < dto.PointsToRedeem)
                    {
                        throw new InvalidOperationException($"Khách hàng chỉ có {customer.RewardPoints} điểm, không đủ để đổi {dto.PointsToRedeem} điểm.");
                    }

                    loyaltyDiscount = dto.PointsToRedeem * 1000;
                    customer.RewardPoints -= dto.PointsToRedeem;

                    await _context.LoyaltyPointLedgers.AddAsync(new LoyaltyPointLedger
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer.Id,
                        TransactionType = LoyaltyTransactionType.RedeemForDiscount,
                        PointsChanged = -dto.PointsToRedeem,
                        BalanceAfter = customer.RewardPoints,
                        Reason = $"Đổi điểm giảm {loyaltyDiscount:N0} ₫ cho đơn {orderNumber}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            decimal totalDiscount = dto.DiscountAmount + loyaltyDiscount;
            decimal totalAmount = Math.Max(0, subTotal - totalDiscount);

            // Kiểm tra thanh toán
            decimal totalPaid = dto.Payments?.Sum(p => p.Amount) ?? 0;
            if (totalPaid < totalAmount)
            {
                throw new InvalidOperationException($"Số tiền khách thanh toán ({totalPaid:N0} ₫) chưa đủ tổng tiền đơn ({totalAmount:N0} ₫).");
            }

            decimal changeAmount = Math.Max(0, totalPaid - totalAmount);

            // 5. Tạo đơn hàng và giao dịch thanh toán
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                IdempotencyKey = dto.IdempotencyKey,
                Source = OrderSource.Pos,
                Status = OrderStatus.Completed,
                StoreId = dto.StoreId,
                ShiftSessionId = dto.ShiftSessionId,
                CustomerId = dto.CustomerId,
                StaffId = shift.StaffId,
                SubTotal = subTotal,
                DiscountAmount = totalDiscount,
                VatAmount = 0,
                TotalAmount = totalAmount,
                PaidAmount = totalPaid,
                ChangeAmount = changeAmount,
                Note = dto.Note?.Trim() ?? string.Empty,
                CreatedBy = operatorName,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var oi in orderItems)
            {
                oi.OrderId = order.Id;
                order.Items.Add(oi);
            }

            // Ghi nhận thanh toán và cập nhật doanh thu ca làm việc
            decimal cashPaid = 0;
            foreach (var payDto in dto.Payments ?? new List<PosPaymentDto>())
            {
                order.Payments.Add(new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    PaymentMethod = payDto.PaymentMethod,
                    Amount = payDto.Amount,
                    TransactionReference = payDto.TransactionReference?.Trim() ?? string.Empty,
                    IsSuccessful = true,
                    PaidAt = DateTime.UtcNow
                });

                if (payDto.PaymentMethod == PaymentMethod.Cash)
                {
                    cashPaid += payDto.Amount;
                }
                else if (payDto.PaymentMethod == PaymentMethod.VietQR)
                {
                    shift.TotalVietQrSales += payDto.Amount;
                }
                else if (payDto.PaymentMethod == PaymentMethod.BankCard)
                {
                    shift.TotalCardSales += payDto.Amount;
                }
            }

            // Với tiền mặt, doanh thu thực nhận = tổng tiền mặt khách đưa - tiền thừa trả lại
            decimal netCashReceived = Math.Max(0, cashPaid - changeAmount);
            shift.TotalCashSales += netCashReceived;
            shift.ExpectedCash = shift.OpeningCash + shift.TotalCashSales - shift.TotalCashPaidOut;
            shift.CompletedOrdersCount++;

            // 6. Tích điểm thưởng cho khách hàng (1 điểm mỗi 10,000 VND chi tiêu)
            int pointsEarned = 0;
            if (customer != null)
            {
                pointsEarned = (int)(totalAmount / 10000);
                if (pointsEarned > 0)
                {
                    customer.RewardPoints += pointsEarned;
                    customer.TotalSpending += totalAmount;

                    await _context.LoyaltyPointLedgers.AddAsync(new LoyaltyPointLedger
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer.Id,
                        OrderId = order.Id,
                        TransactionType = LoyaltyTransactionType.EarnFromOrder,
                        PointsChanged = pointsEarned,
                        BalanceAfter = customer.RewardPoints,
                        Reason = $"Tích {pointsEarned} điểm từ đơn {orderNumber}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Đã hoàn tất thanh toán đơn POS {OrderNumber}: {TotalAmount:N0} ₫. Thu ngân: {Operator}",
                orderNumber, totalAmount, operatorName);

            var receipt = MapToReceipt(order);
            receipt.CustomerPointsEarned = pointsEarned;
            receipt.CustomerCurrentPoints = customer?.RewardPoints ?? 0;
            return receipt;
        }

        public async Task<PosOrderReceiptDto?> GetOrderReceiptAsync(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Store)
                .Include(o => o.Customer)
                .Include(o => o.ShiftSession)
                    .ThenInclude(s => s!.Register)
                .Include(o => o.ShiftSession)
                    .ThenInclude(s => s!.Staff)
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

            return order != null ? MapToReceipt(order) : null;
        }

        private static PosOrderReceiptDto MapToReceipt(Order o)
        {
            return new PosOrderReceiptDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                CreatedAt = o.CreatedAt,
                StoreName = o.Store?.Name ?? "ABCBookShop",
                StoreAddress = o.Store?.Address ?? "",
                StorePhone = o.Store?.Phone ?? "",
                RegisterCode = o.ShiftSession?.Register?.RegisterCode ?? "POS-01",
                CashierName = o.ShiftSession?.Staff?.Name ?? (o.CreatedBy ?? "Thu ngân"),
                CustomerName = o.Customer?.FullName,
                CustomerPhone = o.Customer?.PhoneNumber,
                SubTotal = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                LoyaltyDiscount = 0,
                VatAmount = o.VatAmount,
                TotalAmount = o.TotalAmount,
                PaidAmount = o.PaidAmount,
                ChangeAmount = o.ChangeAmount,
                Items = o.Items.Select(i => new PosReceiptItemDto
                {
                    SKU = i.SKU,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountAmount = i.DiscountAmount,
                    LineTotal = i.LineTotal
                }).ToList(),
                Payments = o.Payments.Select(p => new PosPaymentDto
                {
                    PaymentMethod = p.PaymentMethod,
                    Amount = p.Amount,
                    TransactionReference = p.TransactionReference
                }).ToList()
            };
        }
    }

    public class ShiftReportService : IShiftReportService
    {
        private readonly ABCBookShopContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<ShiftReportService> _logger;
        private readonly HttpClient _httpClient;

        public ShiftReportService(
            ABCBookShopContext context,
            IConfiguration config,
            ILogger<ShiftReportService> logger,
            HttpClient httpClient)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<byte[]> GenerateShiftExcelReportAsync(Guid shiftId)
        {
            var shift = await _context.ShiftSessions
                .Include(s => s.Store)
                .Include(s => s.Register)
                .Include(s => s.Staff)
                .Include(s => s.Orders)
                    .ThenInclude(o => o.Items)
                .FirstOrDefaultAsync(s => s.Id == shiftId)
                ?? throw new KeyNotFoundException("Không tìm thấy ca làm việc.");

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Báo cáo ca POS");

            // Header
            ws.Cell(1, 1).Value = "ABCBOOKSHOP - BIÊN BẢN ĐỐI SOÁT & ĐÓNG CA THU NGÂN";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 6).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = "Mã phiên ca:";
            ws.Cell(3, 2).Value = shift.ShiftCode;
            ws.Cell(3, 4).Value = "Cửa hàng:";
            ws.Cell(3, 5).Value = shift.Store?.Name ?? "";

            ws.Cell(4, 1).Value = "Quầy POS:";
            ws.Cell(4, 2).Value = $"{shift.Register?.RegisterCode} - {shift.Register?.Name}";
            ws.Cell(4, 4).Value = "Thu ngân:";
            ws.Cell(4, 5).Value = shift.Staff?.Name ?? shift.CreatedBy;

            ws.Cell(5, 1).Value = "Giờ mở ca:";
            ws.Cell(5, 2).Value = shift.OpenedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
            ws.Cell(5, 4).Value = "Giờ đóng ca:";
            ws.Cell(5, 5).Value = shift.ClosedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") ?? "Chưa đóng";

            // Bảng tài chính
            ws.Cell(7, 1).Value = "CHỈ SỐ ĐỐI SOÁT TÀI CHÍNH";
            ws.Cell(7, 1).Style.Font.Bold = true;
            ws.Range(7, 1, 7, 3).Merge().Style.Fill.SetBackgroundColor(XLColor.LightGray);

            int r = 8;
            void AddRow(string label, decimal value, XLColor? color = null)
            {
                ws.Cell(r, 1).Value = label;
                ws.Cell(r, 2).Value = value;
                ws.Cell(r, 2).Style.NumberFormat.Format = "#,##0 ₫";
                if (color != null) ws.Cell(r, 2).Style.Font.FontColor = color;
                r++;
            }

            AddRow("Tiền mặt đầu ca (Két):", shift.OpeningCash);
            AddRow("Doanh thu tiền mặt:", shift.TotalCashSales);
            AddRow("Doanh thu chuyển khoản VietQR:", shift.TotalVietQrSales);
            AddRow("Doanh thu quẹt thẻ POS:", shift.TotalCardSales);
            AddRow("Tổng doanh thu bán lẻ:", shift.TotalCashSales + shift.TotalVietQrSales + shift.TotalCardSales, XLColor.DarkGreen);
            AddRow("Chi quỹ tiền mặt tại quầy:", shift.TotalCashPaidOut);
            AddRow("Tiền mặt dự kiến trong két:", shift.ExpectedCash, XLColor.DarkBlue);
            AddRow("Tiền mặt thực tế kiểm đếm:", shift.ActualCashCounted ?? 0);
            AddRow("Chênh lệch tiền mặt:", shift.CashDiscrepancy, shift.CashDiscrepancy != 0 ? XLColor.Red : XLColor.Green);

            if (!string.IsNullOrEmpty(shift.DiscrepancyReason))
            {
                ws.Cell(r, 1).Value = "Lý do giải trình chênh lệch:";
                ws.Cell(r, 2).Value = shift.DiscrepancyReason;
                ws.Range(r, 2, r, 5).Merge();
                r++;
            }

            // Danh sách đơn hàng trong ca
            r += 2;
            ws.Cell(r, 1).Value = "DANH SÁCH ĐƠN HÀNG TRONG PHIÊN CA";
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Range(r, 1, r, 6).Merge().Style.Fill.SetBackgroundColor(XLColor.LightSteelBlue);

            r++;
            var headers = new[] { "Mã đơn hàng", "Thời gian", "Số mặt hàng", "Tổng tiền", "Giảm giá", "Thực thu" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(r, i + 1).Value = headers[i];
                ws.Cell(r, i + 1).Style.Font.Bold = true;
            }

            foreach (var order in shift.Orders)
            {
                r++;
                ws.Cell(r, 1).Value = order.OrderNumber;
                ws.Cell(r, 2).Value = order.CreatedAt.ToLocalTime().ToString("HH:mm:ss");
                ws.Cell(r, 3).Value = order.Items.Sum(i => i.Quantity);
                ws.Cell(r, 4).Value = order.SubTotal;
                ws.Cell(r, 4).Style.NumberFormat.Format = "#,##0 ₫";
                ws.Cell(r, 5).Value = order.DiscountAmount;
                ws.Cell(r, 5).Style.NumberFormat.Format = "#,##0 ₫";
                ws.Cell(r, 6).Value = order.TotalAmount;
                ws.Cell(r, 6).Style.NumberFormat.Format = "#,##0 ₫";
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<bool> SendShiftTelegramAlertAsync(Guid shiftId)
        {
            var botToken = _config["Telegram:BotToken"];
            var chatId = _config["Telegram:ChatId"];

            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId))
            {
                _logger.LogWarning("Telegram BotToken hoặc ChatId chưa được cấu hình. Bỏ qua gửi thông báo Telegram.");
                return false;
            }

            var shift = await _context.ShiftSessions
                .Include(s => s.Store)
                .Include(s => s.Register)
                .Include(s => s.Staff)
                .FirstOrDefaultAsync(s => s.Id == shiftId);

            if (shift == null) return false;

            var totalRev = shift.TotalCashSales + shift.TotalVietQrSales + shift.TotalCardSales;
            var discWarn = shift.CashDiscrepancy != 0
                ? $"⚠️ *Chênh lệch két:* `{shift.CashDiscrepancy:N0} đ`\n📝 *Giải trình:* {shift.DiscrepancyReason}"
                : "✅ *Két tiền khớp 100%*";

            var messageText =
                $"🔔 *BÁO CÁO KẾT CA POS - ABCBOOKSHOP*\n" +
                $"🏪 *Cửa hàng:* {shift.Store?.Name} ({shift.Store?.Code})\n" +
                $"🖥️ *Quầy:* {shift.Register?.RegisterCode} | *Mã ca:* `{shift.ShiftCode}`\n" +
                $"👤 *Thu ngân:* {shift.Staff?.Name ?? shift.CreatedBy}\n" +
                $"⏰ *Mở ca:* {shift.OpenedAt.ToLocalTime():dd/MM/yyyy HH:mm} - *Đóng ca:* {shift.ClosedAt?.ToLocalTime():HH:mm}\n" +
                $"━━━━━━━━━━━━━━━━━━\n" +
                $"💵 *Tiền đầu ca:* `{shift.OpeningCash:N0} đ`\n" +
                $"🧾 *Số đơn hoàn tất:* {shift.CompletedOrdersCount}\n" +
                $"💰 *Tiền mặt thu:* `{shift.TotalCashSales:N0} đ`\n" +
                $"📲 *VietQR thu:* `{shift.TotalVietQrSales:N0} đ`\n" +
                $"💳 *Quẹt thẻ thu:* `{shift.TotalCardSales:N0} đ`\n" +
                $"💎 *Tổng doanh thu:* `{totalRev:N0} đ`\n" +
                $"━━━━━━━━━━━━━━━━━━\n" +
                $"🔢 *Tiền dự kiến két:* `{shift.ExpectedCash:N0} đ`\n" +
                $"🔍 *Thực kiểm đếm:* `{shift.ActualCashCounted ?? 0:N0} đ`\n" +
                $"{discWarn}";

            try
            {
                var payload = new
                {
                    chat_id = chatId,
                    text = messageText,
                    parse_mode = "Markdown"
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"https://api.telegram.org/bot{botToken}/sendMessage", content);

                if (response.IsSuccessStatusCode)
                {
                    shift.IsReportSentTelegram = true;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Gửi thông báo Telegram kết ca {ShiftCode} thành công.", shift.ShiftCode);
                    return true;
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Lỗi khi gửi thông báo Telegram: {Error}", err);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ngoại lệ khi gửi thông báo Telegram cho ca {ShiftCode}", shift.ShiftCode);
                return false;
            }
        }

        public async Task<bool> SendShiftEmailReportAsync(Guid shiftId, string? recipientEmail = null)
        {
            var shift = await _context.ShiftSessions
                .Include(s => s.Store)
                .Include(s => s.Register)
                .Include(s => s.Staff)
                .FirstOrDefaultAsync(s => s.Id == shiftId);

            if (shift == null) return false;

            var targetEmail = recipientEmail ?? _config["MailSettings:StoreManagerEmail"] ?? "manager@abcbookshop.vn";
            var smtpHost = _config["MailSettings:Host"];
            var smtpPort = int.TryParse(_config["MailSettings:Port"], out var port) ? port : 587;
            var smtpUser = _config["MailSettings:Mail"];
            var smtpPass = _config["MailSettings:Password"];

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
            {
                _logger.LogWarning("MailSettings chưa được cấu hình. Bỏ qua gửi Email kết ca.");
                return false;
            }

            try
            {
                var excelBytes = await GenerateShiftExcelReportAsync(shiftId);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("ABCBookShop Automation", smtpUser));
                message.To.Add(new MailboxAddress("Store Manager", targetEmail));
                message.Subject = $"[Báo cáo Kết ca POS] {shift.Store?.Name} - Quầy {shift.Register?.RegisterCode} - {shift.ShiftCode}";

                var builder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <h3>Kính gửi Quản lý cửa hàng,</h3>
                        <p>Hệ thống tự động thông báo kết ca làm việc POS:</p>
                        <ul>
                            <li><strong>Mã ca:</strong> {shift.ShiftCode}</li>
                            <li><strong>Cửa hàng:</strong> {shift.Store?.Name}</li>
                            <li><strong>Quầy thu ngân:</strong> {shift.Register?.RegisterCode}</li>
                            <li><strong>Thu ngân:</strong> {shift.Staff?.Name ?? shift.CreatedBy}</li>
                            <li><strong>Tổng doanh thu:</strong> {(shift.TotalCashSales + shift.TotalVietQrSales + shift.TotalCardSales):N0} VNĐ</li>
                            <li><strong>Chênh lệch két:</strong> {shift.CashDiscrepancy:N0} VNĐ</li>
                        </ul>
                        <p>Chi tiết báo cáo và danh sách hóa đơn đính kèm trong file Excel bên dưới.</p>
                        <p>Trân trọng,<br/>ABCBookShop Retail Platform</p>"
                };

                builder.Attachments.Add($"BaoCao_Ca_{shift.ShiftCode}.xlsx", excelBytes);
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                if (!string.IsNullOrEmpty(smtpPass))
                {
                    await client.AuthenticateAsync(smtpUser, smtpPass);
                }
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                shift.IsReportSentEmail = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Gửi email báo cáo kết ca {ShiftCode} thành công tới {Email}", shift.ShiftCode, targetEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ngoại lệ khi gửi email báo cáo kết ca {ShiftCode}", shift.ShiftCode);
                return false;
            }
        }

        public async Task CheckAndAlertUnclosedShiftsAsync()
        {
            var cutoff = DateTime.UtcNow.AddHours(-14);
            var unclosedShifts = await _context.ShiftSessions
                .Include(s => s.Store)
                .Include(s => s.Register)
                .Include(s => s.Staff)
                .Where(s => s.Status == ShiftStatus.Open && s.OpenedAt < cutoff && !s.IsDeleted)
                .ToListAsync();

            if (!unclosedShifts.Any()) return;

            var botToken = _config["Telegram:BotToken"];
            var chatId = _config["Telegram:ChatId"];
            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId)) return;

            foreach (var shift in unclosedShifts)
            {
                var hoursOpen = (int)(DateTime.UtcNow - shift.OpenedAt).TotalHours;
                var alertMsg =
                    $"🚨 *CẢNH BÁO CA POS CHƯA ĐÓNG QUÁ QUY ĐỊNH*\n" +
                    $"🏪 *Cửa hàng:* {shift.Store?.Name}\n" +
                    $"🖥️ *Quầy:* {shift.Register?.RegisterCode} | *Mã ca:* `{shift.ShiftCode}`\n" +
                    $"👤 *Thu ngân:* {shift.Staff?.Name ?? shift.CreatedBy}\n" +
                    $"⏳ *Thời gian mở:* {hoursOpen} giờ liên tục chưa chốt két!\n" +
                    $"Vui lòng kiểm tra thu ngân và đóng ca theo quy định.";

                try
                {
                    var payload = new { chat_id = chatId, text = alertMsg, parse_mode = "Markdown" };
                    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    await _httpClient.PostAsync($"https://api.telegram.org/bot{botToken}/sendMessage", content);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi cảnh báo ca chưa đóng {ShiftCode}", shift.ShiftCode);
                }
            }
        }
    }
}
