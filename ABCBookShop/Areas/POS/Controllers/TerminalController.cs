using ABCBook.Model;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.POS;
using ABCBookAPIService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCBookShop.Areas.POS.Controllers
{
    [Area("POS")]
    [Authorize]
    public class TerminalController : Controller
    {
        private readonly IPosService _posService;
        private readonly IShiftService _shiftService;
        private readonly ABCBookShopContext _context;

        public TerminalController(
            IPosService posService,
            IShiftService shiftService,
            ABCBookShopContext context)
        {
            _posService = posService;
            _shiftService = shiftService;
            _context = context;
        }

        // GET: /POS/Terminal
        public async Task<IActionResult> Index(Guid? registerId)
        {
            var registers = await _context.POSRegisters
                .Include(r => r.Store)
                .Where(r => r.IsActive && !r.IsDeleted)
                .ToListAsync();

            if (!registers.Any())
            {
                TempData["ErrorMessage"] = "Chưa có Quầy POS nào được khởi tạo trong hệ thống.";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var selectedRegister = registerId.HasValue
                ? registers.FirstOrDefault(r => r.Id == registerId.Value)
                : registers.FirstOrDefault();

            if (selectedRegister == null)
            {
                selectedRegister = registers.First();
            }

            // Kiểm tra xem quầy này đã mở ca chưa
            var activeShift = await _shiftService.GetActiveShiftForRegisterAsync(selectedRegister.Id);
            if (activeShift == null)
            {
                // Chưa mở ca, điều hướng sang màn hình Mở ca
                TempData["ErrorMessage"] = $"Quầy '{selectedRegister.RegisterCode}' chưa mở ca làm việc. Vui lòng kiểm đếm tiền đầu ca và mở ca trước khi bán hàng.";
                return RedirectToAction("Open", "Shift", new { area = "POS", registerId = selectedRegister.Id });
            }

            // Tìm kho bán hàng của cửa hàng tương ứng
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.StoreId == selectedRegister.StoreId && !w.IsDeleted);

            ViewBag.ActiveShift = activeShift;
            ViewBag.Register = selectedRegister;
            ViewBag.Warehouse = warehouse;
            ViewBag.Registers = registers;

            return View();
        }

        // GET: /POS/Terminal/SearchProducts?warehouseId=...&keyword=...
        [HttpGet]
        public async Task<IActionResult> SearchProducts(Guid warehouseId, string? keyword)
        {
            var results = await _posService.SearchProductsForPosAsync(warehouseId, keyword ?? "");
            return Json(results);
        }

        // GET: /POS/Terminal/LookupCustomer?phone=...
        [HttpGet]
        public async Task<IActionResult> LookupCustomer(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return Json(null);
            var customer = await _posService.FindCustomerByPhoneAsync(phone);
            if (customer == null) return Json(null);

            return Json(new
            {
                id = customer.Id,
                code = customer.CustomerCode,
                name = customer.FullName,
                phone = customer.PhoneNumber,
                points = customer.RewardPoints,
                tier = customer.MembershipTier,
                totalSpending = customer.TotalSpending
            });
        }

        // POST: /POS/Terminal/QuickCreateCustomer
        [HttpPost]
        public async Task<IActionResult> QuickCreateCustomer(string fullName, string phone)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
            {
                return BadRequest("Tên và số điện thoại không được để trống.");
            }

            try
            {
                var operatorName = User.Identity?.Name ?? "ThuNgan";
                var customer = await _posService.CreateQuickCustomerAsync(fullName, phone, operatorName);
                return Json(new
                {
                    id = customer.Id,
                    code = customer.CustomerCode,
                    name = customer.FullName,
                    phone = customer.PhoneNumber,
                    points = customer.RewardPoints,
                    tier = customer.MembershipTier
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: /POS/Terminal/Checkout
        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] PosCheckoutDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { success = false, message = errors });
            }

            try
            {
                var operatorName = User.Identity?.Name ?? "ThuNgan";
                var receipt = await _posService.ProcessCheckoutAsync(dto, operatorName);
                return Json(new { success = true, receipt });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET: /POS/Terminal/Receipt/5
        public async Task<IActionResult> Receipt(Guid id)
        {
            var receipt = await _posService.GetOrderReceiptAsync(id);
            if (receipt == null) return NotFound();
            return PartialView("_ReceiptPartial", receipt);
        }
    }
}
