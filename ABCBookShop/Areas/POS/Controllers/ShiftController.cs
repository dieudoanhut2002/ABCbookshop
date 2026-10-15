using ABCBook.Model;
using ABCBookAPIService.DTOs.POS;
using ABCBookAPIService.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCBookShop.Areas.POS.Controllers
{
    [Area("POS")]
    [Authorize]
    public class ShiftController : Controller
    {
        private readonly IShiftService _shiftService;
        private readonly IShiftReportService _reportService;
        private readonly ABCBookShopContext _context;

        public ShiftController(
            IShiftService shiftService,
            IShiftReportService reportService,
            ABCBookShopContext context)
        {
            _shiftService = shiftService;
            _reportService = reportService;
            _context = context;
        }

        // GET: /POS/Shift
        public async Task<IActionResult> Index(Guid? storeId)
        {
            ViewBag.Stores = await _context.Stores.Where(s => s.IsActive && !s.IsDeleted).ToListAsync();
            ViewBag.CurrentStoreId = storeId;

            var shifts = await _shiftService.GetRecentShiftsAsync(storeId, 30);
            return View(shifts);
        }

        // GET: /POS/Shift/Open
        public async Task<IActionResult> Open(Guid? registerId)
        {
            var registers = await _context.POSRegisters
                .Include(r => r.Store)
                .Where(r => r.IsActive && !r.IsDeleted)
                .ToListAsync();

            ViewBag.Registers = registers;

            var defaultRegister = registerId.HasValue
                ? registers.FirstOrDefault(r => r.Id == registerId.Value)
                : registers.FirstOrDefault();

            var model = new ShiftOpenDto
            {
                StoreId = defaultRegister?.StoreId ?? Guid.Empty,
                RegisterId = defaultRegister?.Id ?? Guid.Empty,
                OpeningCash = 1000000 // 1,000,000 đ tiền lẻ ban đầu mặc định
            };

            return View(model);
        }

        // POST: /POS/Shift/Open
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Open(ShiftOpenDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Registers = await _context.POSRegisters
                    .Include(r => r.Store)
                    .Where(r => r.IsActive && !r.IsDeleted)
                    .ToListAsync();
                return View(dto);
            }

            try
            {
                var operatorName = User.Identity?.Name ?? "ThuNgan";
                var shiftId = await _shiftService.OpenShiftAsync(dto, operatorName);

                TempData["SuccessMessage"] = "Mở ca POS thành công! Chúc bạn một ca làm việc thuận lợi và doanh thu tốt.";
                return RedirectToAction("Index", "Terminal", new { area = "POS", registerId = dto.RegisterId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Registers = await _context.POSRegisters
                    .Include(r => r.Store)
                    .Where(r => r.IsActive && !r.IsDeleted)
                    .ToListAsync();
                return View(dto);
            }
        }

        // GET: /POS/Shift/Close/5
        public async Task<IActionResult> Close(Guid id)
        {
            var shift = await _shiftService.GetShiftByIdAsync(id);
            if (shift == null) return NotFound();

            var model = new ShiftCloseDto
            {
                ShiftId = id,
                ActualCashCounted = shift.ExpectedCash
            };

            ViewBag.Shift = shift;
            return View(model);
        }

        // POST: /POS/Shift/Close
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(ShiftCloseDto dto)
        {
            var shift = await _shiftService.GetShiftByIdAsync(dto.ShiftId);
            if (shift == null) return NotFound();

            try
            {
                var operatorName = User.Identity?.Name ?? "ThuNgan";
                var closedShift = await _shiftService.CloseShiftAsync(dto, operatorName);

                // Kích hoạt tác vụ nền tự động gửi báo cáo Telegram & Email qua Hangfire
                BackgroundJob.Enqueue<IShiftReportService>(s => s.SendShiftTelegramAlertAsync(dto.ShiftId));
                BackgroundJob.Enqueue<IShiftReportService>(s => s.SendShiftEmailReportAsync(dto.ShiftId, null));

                TempData["SuccessMessage"] = $"Đóng ca {closedShift.ShiftCode} thành công! Báo cáo đối soát đã được gửi tự động tới Ban quản lý.";
                return RedirectToAction(nameof(Details), new { id = dto.ShiftId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Shift = shift;
                return View(dto);
            }
        }

        // GET: /POS/Shift/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var shift = await _context.ShiftSessions
                .Include(s => s.Store)
                .Include(s => s.Register)
                .Include(s => s.Staff)
                .Include(s => s.Orders)
                    .ThenInclude(o => o.Items)
                .Include(s => s.Orders)
                    .ThenInclude(o => o.Payments)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (shift == null) return NotFound();
            return View(shift);
        }

        // GET: /POS/Shift/ExportExcel/5
        public async Task<IActionResult> ExportExcel(Guid id)
        {
            try
            {
                var bytes = await _reportService.GenerateShiftExcelReportAsync(id);
                var shift = await _shiftService.GetShiftByIdAsync(id);
                var fileName = $"BaoCao_Ca_{shift?.ShiftCode ?? id.ToString()}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi xuất Excel: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: /POS/Shift/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id)
        {
            var operatorName = User.Identity?.Name ?? "QuanLy";
            var success = await _shiftService.ApproveShiftAsync(id, operatorName);

            if (success)
            {
                TempData["SuccessMessage"] = "Đã phê duyệt ca làm việc thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể phê duyệt ca làm việc.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
