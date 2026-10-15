using ABCBook.Model;
using ABCBook.Model.Entity;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Finance;
using ABCBookAPIService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ABCBook.Tests
{
    public class FinanceTests
    {
        private ABCBookShopContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ABCBookShopContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ABCBookShopContext(options);
        }

        [Fact]
        public async Task CreateExpenseVoucher_ShouldRecordOpexAndAssociateCategory()
        {
            // Arrange
            var dbName = "TestDb_Expense_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);

            var store = new Store { Id = Guid.NewGuid(), Code = "STORE-Q1", Name = "Nhà sách Q1", Address = "HCM", Phone = "090", IsActive = true };
            await context.Stores.AddAsync(store);

            var category = new ExpenseCategory { Id = Guid.NewGuid(), Code = "EXP-UTIL", Name = "Điện nước & Internet", IsActive = true };
            await context.ExpenseCategories.AddAsync(category);
            await context.SaveChangesAsync();

            var financeService = new FinanceService(context, NullLogger<FinanceService>.Instance);

            // Act
            var createDto = new ExpenseVoucherCreateDto
            {
                StoreId = store.Id,
                CategoryId = category.Id,
                ExpenseDate = DateTime.UtcNow,
                Amount = 1850000,
                Payee = "Công ty Điện lực Chợ Lớn",
                Description = "Thanh toán tiền điện tháng 10/2026",
                IsRecurring = false
            };

            var voucherId = await financeService.CreateExpenseVoucherAsync(createDto, "KeToan01");

            // Assert
            var voucher = await context.ExpenseVouchers.Include(e => e.Category).FirstOrDefaultAsync(e => e.Id == voucherId);
            Assert.NotNull(voucher);
            Assert.Equal(1850000, voucher.Amount);
            Assert.Equal("Điện nước & Internet", voucher.Category?.Name);
            Assert.Equal(ExpenseStatus.Approved, voucher.Status);
            Assert.StartsWith($"EXP-{store.Code}", voucher.VoucherCode);
        }

        [Fact]
        public async Task GetStoreProfitLoss_ShouldCalculateGrossProfit_Opex_AndOperatingMarginAccurately()
        {
            // Arrange
            var dbName = "TestDb_PnL_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);

            var store = new Store { Id = Guid.NewGuid(), Code = "STORE-Q3", Name = "Nhà sách Q3", Address = "HCM", Phone = "090", IsActive = true };
            await context.Stores.AddAsync(store);

            var categoryRent = new ExpenseCategory { Id = Guid.NewGuid(), Code = "EXP-RENT", Name = "Mặt bằng", IsActive = true };
            var categoryUtil = new ExpenseCategory { Id = Guid.NewGuid(), Code = "EXP-UTIL", Name = "Điện nước", IsActive = true };
            await context.ExpenseCategories.AddRangeAsync(categoryRent, categoryUtil);

            var fromDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            var toDate = new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc);

            // 1. Tạo 2 đơn hàng hoàn tất:
            // Đơn 1: Doanh thu 300,000đ, Giá vốn (COGS) 180,000đ
            // Đơn 2: Doanh thu 200,000đ, Giá vốn (COGS) 120,000đ
            // Tổng Doanh thu = 500,000đ; Tổng COGS = 300,000đ => Lợi nhuận gộp (Gross Profit) = 200,000đ (40%)
            var order1 = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-001",
                StoreId = store.Id,
                Status = OrderStatus.Completed,
                TotalAmount = 300000,
                CreatedAt = new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc)
            };
            order1.Items.Add(new OrderItem { OrderId = order1.Id, Quantity = 3, UnitPrice = 100000, CostPrice = 60000 }); // 3x60k = 180k

            var order2 = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-002",
                StoreId = store.Id,
                Status = OrderStatus.Completed,
                TotalAmount = 200000,
                CreatedAt = new DateTime(2026, 10, 10, 14, 0, 0, DateTimeKind.Utc)
            };
            order2.Items.Add(new OrderItem { OrderId = order2.Id, Quantity = 2, UnitPrice = 100000, CostPrice = 60000 }); // 2x60k = 120k

            await context.Orders.AddRangeAsync(order1, order2);

            // 2. Tạo 2 phiếu chi vận hành (OPEX):
            // Thuê mặt bằng: 50,000đ
            // Tiền điện: 30,000đ
            // Tổng OPEX = 80,000đ => Lợi nhuận thuần (Operating Profit) = 200,000đ - 80,000đ = 120,000đ (24%)
            var exp1 = new ExpenseVoucher
            {
                Id = Guid.NewGuid(),
                VoucherCode = "EXP-01",
                StoreId = store.Id,
                CategoryId = categoryRent.Id,
                Amount = 50000,
                Status = ExpenseStatus.Approved,
                ExpenseDate = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
                Payee = "Chủ nhà",
                Description = "Tiền thuê"
            };

            var exp2 = new ExpenseVoucher
            {
                Id = Guid.NewGuid(),
                VoucherCode = "EXP-02",
                StoreId = store.Id,
                CategoryId = categoryUtil.Id,
                Amount = 30000,
                Status = ExpenseStatus.Approved,
                ExpenseDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                Payee = "EVN",
                Description = "Tiền điện"
            };

            await context.ExpenseVouchers.AddRangeAsync(exp1, exp2);
            await context.SaveChangesAsync();

            var financeService = new FinanceService(context, NullLogger<FinanceService>.Instance);

            // Act
            var pnl = await financeService.GetStoreProfitLossAsync(store.Id, fromDate, toDate);

            // Assert
            Assert.Equal(500000, pnl.GrossRevenue);
            Assert.Equal(300000, pnl.CostOfGoodsSold);
            Assert.Equal(200000, pnl.GrossProfit);
            Assert.Equal(40.00m, pnl.GrossMarginPercent);

            Assert.Equal(80000, pnl.TotalOperatingExpenses);
            Assert.Equal(2, pnl.ExpenseBreakdownByCategory.Count);
            Assert.Equal(50000, pnl.ExpenseBreakdownByCategory["Mặt bằng"]);
            Assert.Equal(30000, pnl.ExpenseBreakdownByCategory["Điện nước"]);

            Assert.Equal(120000, pnl.OperatingProfit); // 200k - 80k = 120k
            Assert.Equal(24.00m, pnl.OperatingMarginPercent); // 120k / 500k = 24%
        }

        [Fact]
        public async Task CalculateMonthlyPayroll_ShouldIncludeBaseSalaryAndSalesCommissions()
        {
            // Arrange
            var dbName = "TestDb_Payroll_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);

            var store = new Store { Id = Guid.NewGuid(), Code = "STORE-Q1", Name = "Nhà sách Q1", Address = "HCM", Phone = "090", IsActive = true };
            await context.Stores.AddAsync(store);

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                StaffCode = "NV010",
                Name = "Trần Thu Ngân Pro",
                StoreID = store.Id,
                BaseSalary = 8000000, // 8,000,000đ lương cứng
                IsQuit = false
            };
            await context.Staffs.AddAsync(staff);

            // Tạo các đơn hàng POS do nhân viên này bán trong tháng 10/2026 tổng cộng 60,000,000đ
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-POS-99",
                StoreId = store.Id,
                StaffId = staff.Id,
                Status = OrderStatus.Completed,
                TotalAmount = 60000000,
                CreatedAt = new DateTime(2026, 10, 15, 11, 0, 0, DateTimeKind.Utc)
            };
            await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();

            var financeService = new FinanceService(context, NullLogger<FinanceService>.Instance);

            // Act: Tính lương tháng 10/2026
            var slips = await financeService.CalculateMonthlyPayrollAsync(store.Id, 10, 2026);

            // Assert
            var slip = slips.FirstOrDefault(s => s.StaffId == staff.Id);
            Assert.NotNull(slip);
            Assert.Equal(8000000, slip.BaseSalary);
            Assert.Equal(60000000, slip.SalesRevenueGenerated);
            Assert.Equal(600000, slip.CommissionAmount); // 1% của 60,000,000 = 600,000đ
            Assert.Equal(500000, slip.Allowances); // Phụ cấp ăn trưa 500k
            Assert.Equal(9100000, slip.NetSalary); // 8,000,000 + 600,000 + 500,000 = 9,100,000đ
        }

        [Fact]
        public async Task GenerateProfitLossExcelReport_ShouldGenerateValidSpreadsheet()
        {
            // Arrange
            var dbName = "TestDb_PnLExcel_" + Guid.NewGuid();
            using var context = CreateInMemoryContext(dbName);

            var store = new Store { Id = Guid.NewGuid(), Code = "STORE-Q1", Name = "Nhà sách Q1", Address = "HCM", Phone = "090", IsActive = true };
            await context.Stores.AddAsync(store);
            await context.SaveChangesAsync();

            var financeService = new FinanceService(context, NullLogger<FinanceService>.Instance);

            // Act
            var excelBytes = await financeService.GenerateProfitLossExcelReportAsync(
                store.Id, new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));

            // Assert
            Assert.NotNull(excelBytes);
            Assert.True(excelBytes.Length > 1000);
            Assert.Equal(0x50, excelBytes[0]); // Magic header PK
            Assert.Equal(0x4B, excelBytes[1]);
        }
    }
}
