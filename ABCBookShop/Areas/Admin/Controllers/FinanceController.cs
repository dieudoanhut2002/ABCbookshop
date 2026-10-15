using ABCBook.Model;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Finance;
using ABCBookAPIService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCBookShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class FinanceController : Controller
    {
        private readonly IFinanceService _financeService;
        private readonly ABCBookShopContext _context;

        public FinanceController(IFinanceService financeService, ABCBookShopContext context)
        {
            _financeService = financeService;
            _context = context;
        }

        // GET: /Admin/Finance (Quản lý Phiếu chi OPEX)
        public async Task<IActionResult> Index(
            Guid? storeId,
            Guid? categoryId,
            ExpenseStatus? status,
            DateTime? fromDate,
            DateTime? toDate,
            int pageIndex = 1)
        {
            ViewBag.Stores = await _context.Stores.Where(s => s.IsActive && !s.IsDeleted).ToListAsync();
            ViewBag.Categories = await _financeService.GetExpenseCategoriesAsync();
            ViewBag.CurrentStoreId = storeId;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentFromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentToDate = toDate?.ToString("yyyy-MM-dd");

            var pagedVouchers = await _financeService.GetExpenseVouchersAsync(
                storeId, categoryId, status, fromDate, toDate, pageIndex, 15);

            return View(pagedVouchers);
        }

        // POST: /Admin/Finance/CreateVoucher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVoucher(ExpenseVoucherCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Thông tin phiếu chi không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var operatorName = User.Identity?.Name ?? "Admin";
                var id = await _financeService.CreateExpenseVoucherAsync(dto, operatorName);
                TempData["SuccessMessage"] = "Lập phiếu chi phí vận hành (OPEX) thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Finance/ApproveVoucher/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVoucher(Guid id)
        {
            var operatorName = User.Identity?.Name ?? "Admin";
            var success = await _financeService.ApproveExpenseVoucherAsync(id, operatorName);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã phê duyệt phiếu chi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể phê duyệt phiếu chi này.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Finance/RejectVoucher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVoucher(Guid id, string reason)
        {
            var operatorName = User.Identity?.Name ?? "Admin";
            var success = await _financeService.RejectExpenseVoucherAsync(id, reason ?? "Từ chối", operatorName);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã từ chối phiếu chi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xử lý từ chối.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Finance/Categories
        public async Task<IActionResult> Categories()
        {
            var categories = await _financeService.GetExpenseCategoriesAsync();
            return View(categories);
        }

        // POST: /Admin/Finance/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(ExpenseCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            {
                TempData["ErrorMessage"] = "Mã và Tên nhóm chi phí không được để trống.";
                return RedirectToAction(nameof(Categories));
            }

            try
            {
                var operatorName = User.Identity?.Name ?? "Admin";
                await _financeService.CreateExpenseCategoryAsync(dto, operatorName);
                TempData["SuccessMessage"] = $"Tạo nhóm chi phí '{dto.Name}' thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Categories));
        }

        // GET: /Admin/Finance/ProfitLoss
        public async Task<IActionResult> ProfitLoss(Guid? storeId, DateTime? fromDate, DateTime? toDate)
        {
            var now = DateTime.UtcNow;
            var from = fromDate ?? new DateTime(now.Year, now.Month, 1); // Đầu tháng
            var to = toDate ?? new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59);

            ViewBag.Stores = await _context.Stores.Where(s => s.IsActive && !s.IsDeleted).ToListAsync();
            ViewBag.CurrentStoreId = storeId;
            ViewBag.CurrentFromDate = from.ToString("yyyy-MM-dd");
            ViewBag.CurrentToDate = to.ToString("yyyy-MM-dd");

            var pnl = await _financeService.GetStoreProfitLossAsync(storeId, from, to);
            return View(pnl);
        }

        // GET: /Admin/Finance/ExportProfitLossExcel
        public async Task<IActionResult> ExportProfitLossExcel(Guid? storeId, DateTime? fromDate, DateTime? toDate)
        {
            var now = DateTime.UtcNow;
            var from = fromDate ?? new DateTime(now.Year, now.Month, 1);
            var to = toDate ?? new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59);

            var bytes = await _financeService.GenerateProfitLossExcelReportAsync(storeId, from, to);
            var fileName = $"BaoCao_PnL_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: /Admin/Finance/Payroll
        public async Task<IActionResult> Payroll(Guid? storeId, int? month, int? year)
        {
            var now = DateTime.UtcNow;
            var m = month ?? now.Month;
            var y = year ?? now.Year;

            ViewBag.Stores = await _context.Stores.Where(s => s.IsActive && !s.IsDeleted).ToListAsync();
            ViewBag.CurrentStoreId = storeId;
            ViewBag.CurrentMonth = m;
            ViewBag.CurrentYear = y;

            var slips = await _financeService.CalculateMonthlyPayrollAsync(storeId, m, y);
            return View(slips);
        }
    }
}
