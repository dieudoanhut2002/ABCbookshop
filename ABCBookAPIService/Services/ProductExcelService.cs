using ABCBook.Model;
using ABCBook.Model.Entity;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace ABCBookAPIService.Services
{
    public class ProductExcelService : IProductExcelService
    {
        private readonly ABCBookShopContext _context;
        private readonly IProductService _productService;

        public ProductExcelService(ABCBookShopContext context, IProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        public async Task<byte[]> GenerateExcelImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Danh mục sản phẩm");

            // 1. Headers
            var headers = new[]
            {
                "Mã SKU (*)",
                "Mã vạch Barcode",
                "Tên sản phẩm (*)",
                "Loại hàng (Sach/VanPhongPham/LuuNiem)",
                "Tên Danh mục",
                "Đơn vị tính",
                "Giá vốn (VND)",
                "Giá bán niêm yết (VND)",
                "Thuế VAT (%)",
                "Tác giả (Nếu là sách)",
                "Nhà xuất bản",
                "Mã ISBN",
                "Năm XB",
                "Ngưỡng tồn thấp"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(23, 162, 184); // Info Blue
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // 2. Dòng dữ liệu mẫu 1 (Sách)
            ws.Cell(2, 1).Value = "BK-DACNHANTAM-01";
            ws.Cell(2, 2).Value = "8934974163588";
            ws.Cell(2, 3).Value = "Đắc Nhân Tâm";
            ws.Cell(2, 4).Value = "Sach";
            ws.Cell(2, 5).Value = "Sách Văn Học & Kỹ Năng";
            ws.Cell(2, 6).Value = "Cuốn";
            ws.Cell(2, 7).Value = 52000;
            ws.Cell(2, 8).Value = 86000;
            ws.Cell(2, 9).Value = 5;
            ws.Cell(2, 10).Value = "Dale Carnegie";
            ws.Cell(2, 11).Value = "NXB Tổng Hợp";
            ws.Cell(2, 12).Value = "978-604-58-7132-4";
            ws.Cell(2, 13).Value = 2024;
            ws.Cell(2, 14).Value = 10;

            // 3. Dòng dữ liệu mẫu 2 (Văn phòng phẩm)
            ws.Cell(3, 1).Value = "ST-BUTBI-TL027";
            ws.Cell(3, 2).Value = ""; // Để trống tự sinh
            ws.Cell(3, 3).Value = "Bút bi Thiên Long TL-027 Mực Xanh";
            ws.Cell(3, 4).Value = "VanPhongPham";
            ws.Cell(3, 5).Value = "Văn Phòng Phẩm";
            ws.Cell(3, 6).Value = "Cây";
            ws.Cell(3, 7).Value = 3500;
            ws.Cell(3, 8).Value = 6000;
            ws.Cell(3, 9).Value = 8;
            ws.Cell(3, 10).Value = "";
            ws.Cell(3, 11).Value = "";
            ws.Cell(3, 12).Value = "";
            ws.Cell(3, 13).Value = "";
            ws.Cell(3, 14).Value = 50;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<ExcelImportResultDto> ImportProductsFromExcelAsync(Stream fileStream, string operatorUserName)
        {
            var result = new ExcelImportResultDto();
            using var workbook = new XLWorkbook(fileStream);
            var ws = workbook.Worksheet(1);
            if (ws == null)
            {
                result.Errors.Add(new ExcelRowErrorDto { RowIndex = 1, ErrorMessage = "File Excel không chứa bất kỳ trang tính (Worksheet) nào." });
                return result;
            }

            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList();
            if (rows == null || !rows.Any())
            {
                result.Errors.Add(new ExcelRowErrorDto { RowIndex = 1, ErrorMessage = "File Excel không có dữ liệu để nhập." });
                return result;
            }

            result.TotalRows = rows.Count;
            var categories = await _context.Categories.Where(c => !c.IsDeleted).ToListAsync();
            var existingSkus = (await _context.Products.Where(p => !p.IsDeleted).Select(p => p.SKU.ToUpper()).ToListAsync()).ToHashSet();
            var existingBarcodes = (await _context.Products.Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Barcode)).Select(p => p.Barcode).ToListAsync()).ToHashSet();

            var newProducts = new List<Product>();
            var fileSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNum = row.RowNumber();
                bool rowHasError = false;

                // Cột 1: SKU (*)
                var sku = row.Cell(1).GetString()?.Trim().ToUpper();
                if (string.IsNullOrWhiteSpace(sku))
                {
                    result.Errors.Add(new ExcelRowErrorDto { RowIndex = rowNum, SKU = "", ColumnName = "Mã SKU", ErrorMessage = "Mã SKU bắt buộc phải có." });
                    rowHasError = true;
                }
                else if (fileSkus.Contains(sku))
                {
                    result.Errors.Add(new ExcelRowErrorDto { RowIndex = rowNum, SKU = sku, ColumnName = "Mã SKU", ErrorMessage = $"Mã SKU '{sku}' bị trùng lặp ngay trong file Excel." });
                    rowHasError = true;
                }
                else if (existingSkus.Contains(sku))
                {
                    result.Errors.Add(new ExcelRowErrorDto { RowIndex = rowNum, SKU = sku, ColumnName = "Mã SKU", ErrorMessage = $"Mã SKU '{sku}' đã tồn tại trong cơ sở dữ liệu." });
                    rowHasError = true;
                }
                else
                {
                    fileSkus.Add(sku);
                }

                // Cột 2: Barcode
                var barcode = row.Cell(2).GetString()?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(barcode) && existingBarcodes.Contains(barcode))
                {
                    result.Errors.Add(new ExcelRowErrorDto { RowIndex = rowNum, SKU = sku ?? "", ColumnName = "Barcode", ErrorMessage = $"Mã vạch '{barcode}' đã được sử dụng bởi sản phẩm khác." });
                    rowHasError = true;
                }

                // Cột 3: Tên sản phẩm (*)
                var title = row.Cell(3).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(title))
                {
                    result.Errors.Add(new ExcelRowErrorDto { RowIndex = rowNum, SKU = sku ?? "", ColumnName = "Tên sản phẩm", ErrorMessage = "Tên sản phẩm không được để trống." });
                    rowHasError = true;
                }

                // Cột 4: Loại hàng
                var typeStr = row.Cell(4).GetString()?.Trim().ToLower() ?? "";
                var productType = ProductType.Book;
                if (typeStr.Contains("vanphong") || typeStr.Contains("vpp") || typeStr.Contains("stationery"))
                {
                    productType = ProductType.Stationery;
                }
                else if (typeStr.Contains("luuniem") || typeStr.Contains("souvenir") || typeStr.Contains("quatang"))
                {
                    productType = ProductType.Souvenir;
                }

                // Cột 5: Tên danh mục
                var categoryName = row.Cell(5).GetString()?.Trim();
                Guid? categoryId = null;
                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    var matchedCat = categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase) || c.Code.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                    if (matchedCat != null)
                    {
                        categoryId = matchedCat.Id;
                    }
                }

                // Cột 6: Đơn vị tính
                var unit = row.Cell(6).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(unit))
                {
                    unit = productType == ProductType.Book ? "Cuốn" : "Cái";
                }

                // Cột 7, 8: Giá vốn & Giá bán
                decimal costPrice = 0;
                decimal sellingPrice = 0;
                if (!row.Cell(7).IsEmpty() && !decimal.TryParse(row.Cell(7).GetString(), out costPrice))
                {
                    result.Errors.Add(new ExcelRowErrorDto { RowIndex = rowNum, SKU = sku ?? "", ColumnName = "Giá vốn", ErrorMessage = "Giá vốn không đúng định dạng số." });
                    rowHasError = true;
                }

                if (!row.Cell(8).IsEmpty() && !decimal.TryParse(row.Cell(8).GetString(), out sellingPrice))
                {
                    result.Errors.Add(new ExcelRowErrorDto { RowIndex = rowNum, SKU = sku ?? "", ColumnName = "Giá bán", ErrorMessage = "Giá bán không đúng định dạng số." });
                    rowHasError = true;
                }

                // Cột 9: VAT
                decimal vatRate = 8;
                if (!row.Cell(9).IsEmpty() && decimal.TryParse(row.Cell(9).GetString(), out var vat))
                {
                    vatRate = vat;
                }

                // Cột 10-13: Chi tiết sách
                var author = row.Cell(10).GetString()?.Trim() ?? "";
                var publisher = row.Cell(11).GetString()?.Trim();
                var isbn = row.Cell(12).GetString()?.Trim();
                int? pubYear = null;
                if (!row.Cell(13).IsEmpty() && int.TryParse(row.Cell(13).GetString(), out var py))
                {
                    pubYear = py;
                }

                // Cột 14: Ngưỡng tồn thấp
                decimal lowStockThreshold = 5;
                if (!row.Cell(14).IsEmpty() && decimal.TryParse(row.Cell(14).GetString(), out var lst))
                {
                    lowStockThreshold = lst;
                }

                if (!rowHasError)
                {
                    var product = new Product
                    {
                        Id = Guid.NewGuid(),
                        SKU = sku!,
                        Barcode = !string.IsNullOrWhiteSpace(barcode) ? barcode : string.Empty, // Sẽ tự sinh nếu rỗng
                        Title = title!,
                        CategoryId = categoryId,
                        ProductType = productType,
                        IsBook = productType == ProductType.Book,
                        Unit = unit,
                        CostPrice = Math.Max(0, costPrice),
                        SellingPrice = Math.Max(0, sellingPrice),
                        VatRate = vatRate,
                        LowStockThreshold = lowStockThreshold,
                        IsActive = true,
                        CreatedBy = operatorUserName,
                        CreatedAt = DateTime.UtcNow
                    };

                    if (product.IsBook)
                    {
                        product.BookDetail = new BookDetail
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            Author = !string.IsNullOrWhiteSpace(author) ? author : "Đang cập nhật",
                            Publisher = publisher,
                            ISBN = isbn,
                            PublicationYear = pubYear,
                            Language = "Tiếng Việt",
                            CoverType = "Bìa mềm"
                        };
                    }

                    newProducts.Add(product);
                }
            }

            result.ErrorCount = result.Errors.Count;

            // Nếu có lỗi, tạo file báo cáo lỗi chi tiết
            if (result.ErrorCount > 0)
            {
                using var errWb = new XLWorkbook();
                var errWs = errWb.Worksheets.Add("Lỗi nhập liệu");

                errWs.Cell(1, 1).Value = "Dòng số";
                errWs.Cell(1, 2).Value = "Mã SKU";
                errWs.Cell(1, 3).Value = "Cột bị lỗi";
                errWs.Cell(1, 4).Value = "Chi tiết lỗi";

                var headerRange = errWs.Range(1, 1, 1, 4);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 53, 69); // Danger Red
                headerRange.Style.Font.FontColor = XLColor.White;

                for (int e = 0; e < result.Errors.Count; e++)
                {
                    var err = result.Errors[e];
                    errWs.Cell(e + 2, 1).Value = err.RowIndex;
                    errWs.Cell(e + 2, 2).Value = err.SKU;
                    errWs.Cell(e + 2, 3).Value = err.ColumnName;
                    errWs.Cell(e + 2, 4).Value = err.ErrorMessage;
                }

                errWs.Columns().AdjustToContents();
                using var errStream = new MemoryStream();
                errWb.SaveAs(errStream);
                result.ErrorReportFileBytes = errStream.ToArray();
                result.ErrorReportFileName = $"BaoCaoLoi_Import_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            }

            // Nếu có sản phẩm hợp lệ, lưu vào Database
            if (newProducts.Any())
            {
                foreach (var prod in newProducts)
                {
                    if (string.IsNullOrWhiteSpace(prod.Barcode))
                    {
                        prod.Barcode = await _productService.GenerateUniqueBarcodeAsync();
                        existingBarcodes.Add(prod.Barcode);
                    }
                }

                await _context.Products.AddRangeAsync(newProducts);

                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserName = operatorUserName,
                    Action = "IMPORT_EXCEL_PRODUCTS",
                    EntityName = nameof(Product),
                    EntityId = $"{newProducts.Count} products",
                    NewValues = $"Đã nhập thành công {newProducts.Count} sản phẩm từ Excel.",
                    Timestamp = DateTime.UtcNow
                };
                await _context.AuditLogs.AddAsync(auditLog);

                await _context.SaveChangesAsync();
                result.SuccessCount = newProducts.Count;
            }

            return result;
        }

        public async Task<byte[]> ExportProductsToExcelAsync(ProductFilterDto? filter = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.BookDetail)
                .Include(p => p.StockBalances)
                .Where(p => !p.IsDeleted);

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.Keyword))
                {
                    var kw = filter.Keyword.Trim();
                    query = query.Where(p => p.Title.Contains(kw) || p.SKU.Contains(kw) || p.Barcode.Contains(kw));
                }
                if (filter.CategoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
                }
                if (filter.ProductType.HasValue)
                {
                    query = query.Where(p => p.ProductType == filter.ProductType.Value);
                }
            }

            var products = await query.OrderBy(p => p.Title).ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Danh mục hàng hóa");

            var headers = new[]
            {
                "STT", "Mã SKU", "Mã vạch Barcode", "Tên sản phẩm", "Danh mục",
                "Loại hàng", "Đơn vị tính", "Giá vốn (VND)", "Giá bán (VND)", "VAT (%)",
                "Tồn kho thực tế", "Tác giả", "Nhà xuất bản", "ISBN", "Trạng thái"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(40, 167, 69); // Success Green
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int r = 0; r < products.Count; r++)
            {
                var p = products[r];
                var rowIdx = r + 2;

                ws.Cell(rowIdx, 1).Value = r + 1;
                ws.Cell(rowIdx, 2).Value = p.SKU;
                ws.Cell(rowIdx, 3).Value = p.Barcode;
                ws.Cell(rowIdx, 4).Value = p.Title;
                ws.Cell(rowIdx, 5).Value = p.Category?.Name ?? "Chưa phân loại";
                ws.Cell(rowIdx, 6).Value = p.ProductType.ToString();
                ws.Cell(rowIdx, 7).Value = p.Unit;

                var costCell = ws.Cell(rowIdx, 8);
                costCell.Value = p.CostPrice;
                costCell.Style.NumberFormat.Format = "#,##0";

                var priceCell = ws.Cell(rowIdx, 9);
                priceCell.Value = p.SellingPrice;
                priceCell.Style.NumberFormat.Format = "#,##0";

                ws.Cell(rowIdx, 10).Value = $"{p.VatRate}%";

                var stockCell = ws.Cell(rowIdx, 11);
                stockCell.Value = p.StockBalances.Sum(sb => sb.Quantity);
                stockCell.Style.NumberFormat.Format = "#,##0";

                ws.Cell(rowIdx, 12).Value = p.BookDetail?.Author ?? "";
                ws.Cell(rowIdx, 13).Value = p.BookDetail?.Publisher ?? "";
                ws.Cell(rowIdx, 14).Value = p.BookDetail?.ISBN ?? "";
                ws.Cell(rowIdx, 15).Value = p.IsActive ? "Đang kinh doanh" : "Ngừng kinh doanh";
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
