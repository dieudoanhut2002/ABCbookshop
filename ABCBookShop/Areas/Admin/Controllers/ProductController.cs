using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCBookShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IProductExcelService _excelService;

        public ProductController(
            IProductService productService,
            ICategoryService categoryService,
            IProductExcelService excelService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _excelService = excelService;
        }

        // GET: /Admin/Product
        public async Task<IActionResult> Index(ProductFilterDto filter)
        {
            var pagedProducts = await _productService.GetPagedProductsAsync(filter);
            ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.CurrentFilter = filter;
            return View(pagedProducts);
        }

        // GET: /Admin/Product/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
            var dto = new ProductCreateUpdateDto
            {
                AutoGenerateBarcode = true,
                VatRate = 8,
                Unit = "Cuốn"
            };
            return View(dto);
        }

        // POST: /Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
                return View(dto);
            }

            try
            {
                var operatorName = User.Identity?.Name ?? "AdminUser";
                await _productService.CreateProductAsync(dto, operatorName);
                TempData["SuccessMessage"] = $"Đã tạo thành công sản phẩm: {dto.Title}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
                return View(dto);
            }
        }

        // GET: /Admin/Product/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
            var dto = new ProductCreateUpdateDto
            {
                Id = product.Id,
                SKU = product.SKU,
                Barcode = product.Barcode,
                Title = product.Title,
                Description = product.Description,
                CategoryId = product.CategoryId,
                ProductType = product.ProductType,
                Unit = product.Unit,
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                VatRate = product.VatRate,
                LowStockThreshold = product.LowStockThreshold,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                ISBN = product.BookDetail?.ISBN,
                Author = product.BookDetail?.Author,
                Publisher = product.BookDetail?.Publisher,
                PublicationYear = product.BookDetail?.PublicationYear,
                PageCount = product.BookDetail?.PageCount,
                Language = product.BookDetail?.Language,
                CoverType = product.BookDetail?.CoverType
            };

            return View(dto);
        }

        // POST: /Admin/Product/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ProductCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
                return View(dto);
            }

            try
            {
                var operatorName = User.Identity?.Name ?? "AdminUser";
                var updated = await _productService.UpdateProductAsync(id, dto, operatorName);
                if (!updated)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để cập nhật.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["SuccessMessage"] = $"Đã cập nhật sản phẩm: {dto.Title}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
                return View(dto);
            }
        }

        // POST: /Admin/Product/ToggleStatus/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var operatorName = User.Identity?.Name ?? "AdminUser";
            var success = await _productService.ToggleProductStatusAsync(id, operatorName);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã thay đổi trạng thái kinh doanh của sản phẩm.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm yêu cầu.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Product/DownloadTemplate
        public async Task<IActionResult> DownloadTemplate()
        {
            var bytes = await _excelService.GenerateExcelImportTemplateAsync();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau_Nhap_SanPham_ABCBookShop.xlsx");
        }

        // GET: /Admin/Product/ExportExcel
        public async Task<IActionResult> ExportExcel(ProductFilterDto filter)
        {
            var bytes = await _excelService.ExportProductsToExcelAsync(filter);
            var fileName = $"DanhMuc_SanPham_ABCBookShop_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // POST: /Admin/Product/ImportExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile? excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn một file Excel (.xlsx) hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var extension = Path.GetExtension(excelFile.FileName).ToLower();
            if (extension != ".xlsx")
            {
                TempData["ErrorMessage"] = "Hệ thống chỉ chấp nhận định dạng file .xlsx.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var operatorName = User.Identity?.Name ?? "AdminUser";
                using var stream = excelFile.OpenReadStream();
                var result = await _excelService.ImportProductsFromExcelAsync(stream, operatorName);

                if (result.ErrorCount == 0 && result.SuccessCount > 0)
                {
                    TempData["SuccessMessage"] = $"Nhập Excel thành công toàn bộ {result.SuccessCount} sản phẩm!";
                }
                else if (result.SuccessCount > 0 && result.ErrorCount > 0)
                {
                    TempData["WarningMessage"] = $"Đã nhập thành công {result.SuccessCount} sản phẩm, nhưng phát hiện {result.ErrorCount} dòng có lỗi.";
                    if (result.ErrorReportFileBytes != null)
                    {
                        HttpContext.Session.Set("LastErrorReportBytes", result.ErrorReportFileBytes);
                        HttpContext.Session.SetString("LastErrorReportFileName", result.ErrorReportFileName ?? "BaoCaoLoi.xlsx");
                        TempData["HasErrorReport"] = true;
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = $"Nhập thất bại! Phát hiện {result.ErrorCount} dòng lỗi. Không có sản phẩm nào được nạp.";
                    if (result.ErrorReportFileBytes != null)
                    {
                        HttpContext.Session.Set("LastErrorReportBytes", result.ErrorReportFileBytes);
                        HttpContext.Session.SetString("LastErrorReportFileName", result.ErrorReportFileName ?? "BaoCaoLoi.xlsx");
                        TempData["HasErrorReport"] = true;
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi xử lý file Excel: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Admin/Product/DownloadLastErrorReport
        public IActionResult DownloadLastErrorReport()
        {
            var bytes = HttpContext.Session.Get("LastErrorReportBytes");
            var fileName = HttpContext.Session.GetString("LastErrorReportFileName") ?? "BaoCaoLoi_Import.xlsx";

            if (bytes == null || bytes.Length == 0)
            {
                TempData["ErrorMessage"] = "Không tìm thấy báo cáo lỗi để tải xuống.";
                return RedirectToAction(nameof(Index));
            }

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
