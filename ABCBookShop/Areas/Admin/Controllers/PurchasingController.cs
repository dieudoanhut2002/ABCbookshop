using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.DTOs.Purchasing;
using ABCBookAPIService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCBookShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class PurchasingController : Controller
    {
        private readonly IPurchasingService _purchasingService;
        private readonly IInventoryService _inventoryService;
        private readonly IProductService _productService;

        public PurchasingController(
            IPurchasingService purchasingService,
            IInventoryService inventoryService,
            IProductService productService)
        {
            _purchasingService = purchasingService;
            _inventoryService = inventoryService;
            _productService = productService;
        }

        // GET: /Admin/Purchasing
        public async Task<IActionResult> Index(Guid? warehouseId)
        {
            ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
            ViewBag.CurrentWarehouseId = warehouseId;

            var notes = await _purchasingService.GetGoodsReceiptNotesAsync(warehouseId);
            return View(notes);
        }

        // GET: /Admin/Purchasing/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Providers = await _purchasingService.GetProvidersAsync();
            ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
            var productsResult = await _productService.GetPagedProductsAsync(new ProductFilterDto { PageSize = 200 });
            ViewBag.Products = productsResult.Items;

            var model = new GoodsReceiptNoteCreateDto
            {
                Items = new List<ReceiptItemInputDto>
                {
                    new ReceiptItemInputDto { Quantity = 1, UnitCost = 0 }
                }
            };
            return View(model);
        }

        // POST: /Admin/Purchasing/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GoodsReceiptNoteCreateDto dto)
        {
            if (dto.Items == null || !dto.Items.Any(i => i.ProductId != Guid.Empty && i.Quantity > 0))
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất một sản phẩm hợp lệ có số lượng > 0.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Providers = await _purchasingService.GetProvidersAsync();
                ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
                var productsResult = await _productService.GetPagedProductsAsync(new ProductFilterDto { PageSize = 200 });
                ViewBag.Products = productsResult.Items;
                return View(dto);
            }

            try
            {
                dto.Items = dto.Items.Where(i => i.ProductId != Guid.Empty && i.Quantity > 0).ToList();
                var operatorName = User.Identity?.Name ?? "Admin";
                var grnId = await _purchasingService.CreateGoodsReceiptNoteAsync(dto, operatorName);

                TempData["SuccessMessage"] = "Nhập hàng từ Nhà cung cấp thành công! Tồn kho và công nợ đã được ghi nhận tự động vào Sổ cái kép.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi khi lưu phiếu nhập: {ex.Message}");
                ViewBag.Providers = await _purchasingService.GetProvidersAsync();
                ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
                var productsResult = await _productService.GetPagedProductsAsync(new ProductFilterDto { PageSize = 200 });
                ViewBag.Products = productsResult.Items;
                return View(dto);
            }
        }

        // GET: /Admin/Purchasing/Suppliers
        public async Task<IActionResult> Suppliers()
        {
            var suppliers = await _purchasingService.GetProvidersAsync();
            return View(suppliers);
        }

        // POST: /Admin/Purchasing/CreateSupplier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier(ProviderDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            {
                TempData["ErrorMessage"] = "Mã và Tên Nhà cung cấp không được để trống.";
                return RedirectToAction(nameof(Suppliers));
            }

            try
            {
                var operatorName = User.Identity?.Name ?? "Admin";
                await _purchasingService.CreateProviderAsync(dto, operatorName);
                TempData["SuccessMessage"] = $"Thêm mới Nhà cung cấp '{dto.Name}' thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Suppliers));
        }
    }
}
