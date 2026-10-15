using ABCBook.Model;
using ABCBook.Model.Entity;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.DTOs.Finance;
using ABCBookAPIService.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ABCBookAPIService.Services
{
    public class FinanceService : IFinanceService
    {
        private readonly ABCBookShopContext _context;
        private readonly ILogger<FinanceService> _logger;

        public FinanceService(ABCBookShopContext context, ILogger<FinanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ExpenseCategoryDto>> GetExpenseCategoriesAsync()
        {
            return await _context.ExpenseCategories
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new ExpenseCategoryDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Description = c.Description,
                    IsDefault = c.IsDefault,
                    IsActive = c.IsActive
                })
                .ToListAsync();
        }

        public async Task<Guid> CreateExpenseCategoryAsync(ExpenseCategoryDto dto, string operatorName)
        {
            var code = dto.Code.Trim().ToUpper();
            var exists = await _context.ExpenseCategories.AnyAsync(c => c.Code == code && !c.IsDeleted);
            if (exists)
            {
                throw new InvalidOperationException($"Mã nhóm chi phí '{code}' đã tồn tại.");
            }

            var category = new ExpenseCategory
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                IsActive = true,
                CreatedBy = operatorName,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ExpenseCategories.AddAsync(category);
            await _context.SaveChangesAsync();
            return category.Id;
        }

        public async Task<PagedResult<ExpenseVoucherDto>> GetExpenseVouchersAsync(
            Guid? storeId,
            Guid? categoryId,
            ExpenseStatus? status,
            DateTime? fromDate,
            DateTime? toDate,
            int pageIndex,
            int pageSize)
        {
            var query = _context.ExpenseVouchers
                .Include(e => e.Store)
                .Include(e => e.Category)
                .Where(e => !e.IsDeleted);

            if (storeId.HasValue) query = query.Where(e => e.StoreId == storeId.Value);
            if (categoryId.HasValue) query = query.Where(e => e.CategoryId == categoryId.Value);
            if (status.HasValue) query = query.Where(e => e.Status == status.Value);
            if (fromDate.HasValue) query = query.Where(e => e.ExpenseDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(e => e.ExpenseDate <= toDate.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(e => e.ExpenseDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new ExpenseVoucherDto
                {
                    Id = e.Id,
                    VoucherCode = e.VoucherCode,
                    StoreId = e.StoreId,
                    StoreName = e.Store != null ? e.Store.Name : "",
                    CategoryId = e.CategoryId,
                    CategoryName = e.Category != null ? e.Category.Name : "",
                    ExpenseDate = e.ExpenseDate,
                    Amount = e.Amount,
                    Payee = e.Payee,
                    Description = e.Description,
                    AttachmentUrl = e.AttachmentUrl,
                    Status = e.Status,
                    CreatedBy = e.CreatedBy,
                    CreatedAt = e.CreatedAt,
                    ApprovedAt = e.ApprovedAt
                })
                .ToListAsync();

            return new PagedResult<ExpenseVoucherDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<Guid> CreateExpenseVoucherAsync(ExpenseVoucherCreateDto dto, string operatorName)
        {
            var store = await _context.Stores.FindAsync(dto.StoreId)
                ?? throw new KeyNotFoundException("Không tìm thấy Cửa hàng.");

            var category = await _context.ExpenseCategories.FindAsync(dto.CategoryId)
                ?? throw new KeyNotFoundException("Không tìm thấy Nhóm chi phí.");

            var voucherCode = $"EXP-{store.Code}-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 9999)}";

            var voucher = new ExpenseVoucher
            {
                Id = Guid.NewGuid(),
                VoucherCode = voucherCode,
                StoreId = dto.StoreId,
                CategoryId = dto.CategoryId,
                ExpenseDate = dto.ExpenseDate,
                Amount = dto.Amount,
                Payee = dto.Payee.Trim(),
                Description = dto.Description.Trim(),
                IsRecurring = dto.IsRecurring,
                Status = ExpenseStatus.Approved, // Mặc định duyệt chi nếu lập bởi nhân viên được cấp quyền
                ApprovedAt = DateTime.UtcNow,
                CreatedBy = operatorName,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ExpenseVouchers.AddAsync(voucher);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tạo phiếu chi vận hành {Code}: {Amount:N0} ₫ cho '{Payee}' tại cửa hàng {Store}",
                voucherCode, dto.Amount, dto.Payee, store.Name);

            return voucher.Id;
        }

        public async Task<bool> ApproveExpenseVoucherAsync(Guid voucherId, string operatorName)
        {
            var voucher = await _context.ExpenseVouchers.FindAsync(voucherId);
            if (voucher == null || voucher.Status != ExpenseStatus.Pending) return false;

            voucher.Status = ExpenseStatus.Approved;
            voucher.ApprovedAt = DateTime.UtcNow;
            voucher.UpdatedBy = operatorName;
            voucher.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectExpenseVoucherAsync(Guid voucherId, string reason, string operatorName)
        {
            var voucher = await _context.ExpenseVouchers.FindAsync(voucherId);
            if (voucher == null || voucher.Status != ExpenseStatus.Pending) return false;

            voucher.Status = ExpenseStatus.Rejected;
            voucher.Description += $" [Từ chối bởi {operatorName}: {reason}]";
            voucher.UpdatedBy = operatorName;
            voucher.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<StoreProfitLossDto> GetStoreProfitLossAsync(Guid? storeId, DateTime fromDate, DateTime toDate)
        {
            var storeName = "Toàn hệ thống chuỗi";
            if (storeId.HasValue)
            {
                var store = await _context.Stores.FindAsync(storeId.Value);
                if (store != null) storeName = store.Name;
            }

            // 1. Truy vấn doanh thu & giá vốn từ Đơn hàng hoàn tất
            var ordersQuery = _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= fromDate && o.CreatedAt <= toDate && !o.IsDeleted);

            if (storeId.HasValue) ordersQuery = ordersQuery.Where(o => o.StoreId == storeId.Value);

            var orders = await ordersQuery.ToListAsync();

            decimal grossRevenue = orders.Sum(o => o.TotalAmount);
            int ordersCount = orders.Count;
            decimal itemsSold = orders.SelectMany(o => o.Items).Sum(i => i.Quantity);
            decimal cogs = orders.SelectMany(o => o.Items).Sum(i => i.Quantity * i.CostPrice);

            // 2. Truy vấn chi phí vận hành (OPEX)
            var expenseQuery = _context.ExpenseVouchers
                .Include(e => e.Category)
                .Where(e => (e.Status == ExpenseStatus.Approved || e.Status == ExpenseStatus.Paid)
                            && e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate && !e.IsDeleted);

            if (storeId.HasValue) expenseQuery = expenseQuery.Where(e => e.StoreId == storeId.Value);

            var expenses = await expenseQuery.ToListAsync();

            decimal totalExpenses = expenses.Sum(e => e.Amount);
            var breakdown = expenses
                .GroupBy(e => e.Category?.Name ?? "Chi phí khác")
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

            return new StoreProfitLossDto
            {
                StoreId = storeId,
                StoreName = storeName,
                FromDate = fromDate,
                ToDate = toDate,
                GrossRevenue = grossRevenue,
                TotalOrdersCount = ordersCount,
                TotalItemsSold = itemsSold,
                CostOfGoodsSold = cogs,
                TotalOperatingExpenses = totalExpenses,
                ExpenseBreakdownByCategory = breakdown
            };
        }

        public async Task<byte[]> GenerateProfitLossExcelReportAsync(Guid? storeId, DateTime fromDate, DateTime toDate)
        {
            var pnl = await GetStoreProfitLossAsync(storeId, fromDate, toDate);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Báo cáo P&L");

            // Header
            ws.Cell(1, 1).Value = "ABCBOOKSHOP - BÁO CÁO KẾT QUẢ HOẠT ĐỘNG KINH DOANH (P&L)";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 4).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = "Phạm vi:";
            ws.Cell(3, 2).Value = pnl.StoreName;
            ws.Cell(4, 1).Value = "Kỳ báo cáo:";
            ws.Cell(4, 2).Value = $"Từ {fromDate:dd/MM/yyyy} đến {toDate:dd/MM/yyyy}";

            // P&L Table
            int r = 6;
            ws.Cell(r, 1).Value = "CHỈ TIÊU TÀI CHÍNH";
            ws.Cell(r, 2).Value = "SỐ TIỀN (VNĐ)";
            ws.Cell(r, 3).Value = "TỶ LỆ (%)";
            ws.Range(r, 1, r, 3).Style.Font.Bold = true;
            ws.Range(r, 1, r, 3).Style.Fill.SetBackgroundColor(XLColor.LightSteelBlue);

            void WriteMetric(string name, decimal amount, decimal? percent = null, bool isBold = false, XLColor? color = null)
            {
                r++;
                ws.Cell(r, 1).Value = name;
                ws.Cell(r, 2).Value = amount;
                ws.Cell(r, 2).Style.NumberFormat.Format = "#,##0 ₫";
                if (percent.HasValue)
                {
                    ws.Cell(r, 3).Value = percent.Value / 100m;
                    ws.Cell(r, 3).Style.NumberFormat.Format = "0.00%";
                }
                if (isBold) ws.Row(r).Style.Font.Bold = true;
                if (color != null) ws.Cell(r, 2).Style.Font.FontColor = color;
            }

            WriteMetric("1. DOANH THU BÁN LẺ THUẦN", pnl.GrossRevenue, 100m, true);
            WriteMetric("   - Số lượng đơn hàng:", pnl.TotalOrdersCount);
            WriteMetric("   - Số lượng sản phẩm bán ra:", pnl.TotalItemsSold);
            WriteMetric("2. GIÁ VỐN HÀNG BÁN (COGS)", pnl.CostOfGoodsSold, pnl.GrossRevenue > 0 ? (pnl.CostOfGoodsSold / pnl.GrossRevenue) * 100 : 0);
            WriteMetric("3. LỢI NHUẬN GỘP (GROSS PROFIT)", pnl.GrossProfit, pnl.GrossMarginPercent, true, XLColor.DarkGreen);

            r++;
            ws.Cell(r, 1).Value = "4. CHI PHÍ VẬN HÀNH (OPEX):";
            ws.Cell(r, 1).Style.Font.Bold = true;

            foreach (var kvp in pnl.ExpenseBreakdownByCategory)
            {
                decimal expPct = pnl.GrossRevenue > 0 ? (kvp.Value / pnl.GrossRevenue) * 100 : 0;
                WriteMetric($"   - {kvp.Key}", kvp.Value, expPct);
            }

            WriteMetric("TỔNG CHI PHÍ VẬN HÀNH", pnl.TotalOperatingExpenses, pnl.GrossRevenue > 0 ? (pnl.TotalOperatingExpenses / pnl.GrossRevenue) * 100 : 0, true, XLColor.DarkOrange);
            WriteMetric("5. LỢI NHUẬN THUẦN TỪ KINH DOANH (OPERATING PROFIT)", pnl.OperatingProfit, pnl.OperatingMarginPercent, true, pnl.OperatingProfit >= 0 ? XLColor.DarkBlue : XLColor.Red);

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<List<StaffPayrollSlipDto>> CalculateMonthlyPayrollAsync(Guid? storeId, int month, int year)
        {
            var fromDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var toDate = fromDate.AddMonths(1).AddTicks(-1);

            var staffQuery = _context.Staffs
                .Include(s => s.Store)
                .Where(s => !s.IsQuit && !s.IsDeleted);

            if (storeId.HasValue) staffQuery = staffQuery.Where(s => s.StoreID == storeId.Value);

            var staffs = await staffQuery.ToListAsync();
            var results = new List<StaffPayrollSlipDto>();

            foreach (var staff in staffs)
            {
                // Doanh thu đơn hàng POS do nhân viên này bán trong tháng
                var staffRevenue = await _context.Orders
                    .Where(o => o.StaffId == staff.Id && o.Status == OrderStatus.Completed
                                && o.CreatedAt >= fromDate && o.CreatedAt <= toDate && !o.IsDeleted)
                    .SumAsync(o => o.TotalAmount);

                results.Add(new StaffPayrollSlipDto
                {
                    StaffId = staff.Id,
                    StaffCode = staff.StaffCode,
                    StaffName = staff.Name,
                    StoreName = staff.Store?.Name ?? "",
                    Month = month,
                    Year = year,
                    BaseSalary = staff.BaseSalary > 0 ? staff.BaseSalary : 7000000, // Mặc định 7,000,000đ nếu chưa thiết lập
                    SalesRevenueGenerated = staffRevenue,
                    CommissionRate = 0.01m, // 1% hoa hồng doanh thu
                    Allowances = 500000, // Phụ cấp ăn trưa mặc định 500k
                    Deductions = 0
                });
            }

            return results;
        }
    }
}
