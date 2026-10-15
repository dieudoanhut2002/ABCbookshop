using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.DTOs.Inventory;
using ABCBookAPIService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ABCBookShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly ITransferService _transferService;
        private readonly IProductService _productService;

        public InventoryController(
            IInventoryService inventoryService,
            ITransferService transferService,
            IProductService productService)
        {
            _inventoryService = inventoryService;
            _transferService = transferService;
            _productService = productService;
        }

        // GET: /Admin/Inventory
        public async Task<IActionResult> Index(Guid? warehouseId, string? keyword, bool? onlyLowStock, int pageIndex = 1)
        {
            ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
            ViewBag.CurrentWarehouseId = warehouseId;
            ViewBag.CurrentKeyword = keyword;
            ViewBag.CurrentOnlyLowStock = onlyLowStock;

            var pagedResult = await _inventoryService.GetStockBalancesAsync(warehouseId, keyword, onlyLowStock, pageIndex, 15);
            return View(pagedResult);
        }

        // GET: /Admin/Inventory/Ledger
        public async Task<IActionResult> Ledger(Guid? warehouseId, Guid? productId, DateTime? fromDate, DateTime? toDate, int pageIndex = 1)
        {
            ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
            ViewBag.CurrentWarehouseId = warehouseId;
            ViewBag.CurrentProductId = productId;
            ViewBag.CurrentFromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentToDate = toDate?.ToString("yyyy-MM-dd");

            var pagedTransactions = await _inventoryService.GetStockTransactionsAsync(warehouseId, productId, fromDate, toDate, pageIndex, 20);
            return View(pagedTransactions);
        }

        // GET: /Admin/Inventory/Adjust
        public async Task<IActionResult> Adjust(Guid? warehouseId, Guid? productId)
        {
            ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
            var productsResult = await _productService.GetPagedProductsAsync(new ProductFilterDto { PageSize = 100 });
            ViewBag.Products = productsResult.Items;

            var dto = new StockAdjustmentDto
            {
                WarehouseId = warehouseId ?? Guid.Empty,
                ProductId = productId ?? Guid.Empty,
                AdjustmentType = StockTransactionType.DamagedLoss,
                IsReduction = true,
                Quantity = 1
            };

            return View(dto);
        }

        // POST: /Admin/Inventory/Adjust
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(StockAdjustmentDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
                var productsResult = await _productService.GetPagedProductsAsync(new ProductFilterDto { PageSize = 100 });
                ViewBag.Products = productsResult.Items;
                return View(dto);
            }

            try
            {
                var operatorName = User.Identity?.Name ?? "AdminUser";
                await _inventoryService.AdjustStockAsync(dto, operatorName);
                TempData["SuccessMessage"] = "Đã thực hiện điều chỉnh tồn kho thành công và ghi nhận vào sổ cái.";
                return RedirectToAction(nameof(Ledger), new { warehouseId = dto.WarehouseId, productId = dto.ProductId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
                var productsResult = await _productService.GetPagedProductsAsync(new ProductFilterDto { PageSize = 100 });
                ViewBag.Products = productsResult.Items;
                return View(dto);
            }
        }

        // GET: /Admin/Inventory/Transfers
        public async Task<IActionResult> Transfers(Guid? fromWarehouseId, Guid? toWarehouseId, TransferStatus? status)
        {
            ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
            ViewBag.CurrentFromWarehouseId = fromWarehouseId;
            ViewBag.CurrentToWarehouseId = toWarehouseId;
            ViewBag.CurrentStatus = status;

            var transfers = await _transferService.GetTransferOrdersAsync(fromWarehouseId, toWarehouseId, status);
            return View(transfers);
        }

        // GET: /Admin/Inventory/CreateTransfer
        public async Task<IActionResult> CreateTransfer()
        {
            ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
            var productsResult = await _productService.GetPagedProductsAsync(new ProductFilterDto { PageSize = 100 });
            ViewBag.Products = productsResult.Items;

            return View(new TransferOrderCreateDto());
        }

        // POST: /Admin/Inventory/CreateTransfer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTransfer(TransferOrderCreateDto dto)
        {
            if (dto.FromWarehouseId == dto.ToWarehouseId)
            {
                ModelState.AddModelError("", "Kho gửi và kho nhận không được trùng nhau.");
            }

            if (dto.Items == null || !dto.Items.Any(i => i.Quantity > 0))
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một mặt hàng và số lượng chuyển lớn hơn 0.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
                var productsResult = await _productService.GetPagedProductsAsync(new ProductFilterDto { PageSize = 100 });
                ViewBag.Products = productsResult.Items;
                return View(dto);
            }

            try
            {
                var operatorName = User.Identity?.Name ?? "AdminUser";
                dto.Items = dto.Items.Where(i => i.Quantity > 0).ToList();
                var transferId = await _transferService.CreateTransferOrderAsync(dto, operatorName);
                TempData["SuccessMessage"] = "Đã lập yêu cầu chuyển kho thành công (Trạng thái: Dự thảo).";
                return RedirectToAction(nameof(TransferDetails), new { id = transferId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Warehouses = await _inventoryService.GetWarehousesAsync();
                var productsResult = await _productService.GetPagedProductsAsync(new ProductFilterDto { PageSize = 100 });
                ViewBag.Products = productsResult.Items;
                return View(dto);
            }
        }

        // GET: /Admin/Inventory/TransferDetails/{id}
        public async Task<IActionResult> TransferDetails(Guid id)
        {
            var transfer = await _transferService.GetTransferOrderByIdAsync(id);
            if (transfer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn chuyển kho.";
                return RedirectToAction(nameof(Transfers));
            }

            return View(transfer);
        }

        // POST: /Admin/Inventory/Dispatch/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dispatch(Guid id)
        {
            try
            {
                var operatorName = User.Identity?.Name ?? "AdminUser";
                await _transferService.DispatchTransferOrderAsync(id, operatorName);
                TempData["SuccessMessage"] = "Đã xuất kho thành công! Hàng hóa đang trong quá trình vận chuyển (In-Transit).";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi xuất kho vận chuyển: {ex.Message}";
            }

            return RedirectToAction(nameof(TransferDetails), new { id });
        }

        // POST: /Admin/Inventory/Receive/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(Guid id, Dictionary<Guid, decimal> receivedQuantities)
        {
            try
            {
                var operatorName = User.Identity?.Name ?? "AdminUser";
                await _transferService.ReceiveTransferOrderAsync(id, receivedQuantities, operatorName);
                TempData["SuccessMessage"] = "Đã nhập kho đích thành công và cập nhật tồn kho toàn chuỗi!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi nhận hàng nhập kho: {ex.Message}";
            }

            return RedirectToAction(nameof(TransferDetails), new { id });
        }
    }
}
