using ABCBook.Model;
using ABCBook.Model.Entity;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.Services;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ABCBook.Tests
{
    public class CatalogTests
    {
        private ABCBookShopContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ABCBookShopContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ABCBookShopContext(options);
        }

        [Fact]
        public async Task AutoGenerateBarcode_ShouldFollowCorrectFormatAndBeUnique()
        {
            // Arrange
            using var context = GetInMemoryDbContext("TestDb_Barcode");
            var productService = new ProductService(context);

            // Act
            var barcode1 = await productService.GenerateUniqueBarcodeAsync();
            var barcode2 = await productService.GenerateUniqueBarcodeAsync();

            // Assert
            var expectedPrefix = $"ABC{DateTime.UtcNow:yyMM}";
            Assert.StartsWith(expectedPrefix, barcode1);
            Assert.StartsWith(expectedPrefix, barcode2);
            Assert.Equal(13, barcode1.Length); // ABC (3) + yyMM (4) + 6 digits = 13 chars
            Assert.NotEqual(barcode1, barcode2);
        }

        [Fact]
        public async Task CreateProduct_WithDuplicateSku_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = GetInMemoryDbContext("TestDb_DuplicateSku");
            var productService = new ProductService(context);

            var dto1 = new ProductCreateUpdateDto
            {
                SKU = "BK-TEST-001",
                Title = "Sách Test 1",
                CostPrice = 50000,
                SellingPrice = 80000,
                ProductType = ProductType.Book,
                Unit = "Cuốn"
            };

            await productService.CreateProductAsync(dto1, "Tester");

            var dto2 = new ProductCreateUpdateDto
            {
                SKU = "BK-TEST-001", // Duplicate
                Title = "Sách Test Trùng SKU",
                CostPrice = 60000,
                SellingPrice = 90000,
                ProductType = ProductType.Book,
                Unit = "Cuốn"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                productService.CreateProductAsync(dto2, "Tester"));
        }

        [Fact]
        public async Task ProductExcelService_ImportExcel_ShouldDetectMissingSkuAndDuplicate()
        {
            // Arrange
            using var context = GetInMemoryDbContext("TestDb_ExcelImport");
            var productService = new ProductService(context);
            var excelService = new ProductExcelService(context, productService);

            // Tạo file Excel giả lập bằng ClosedXML trong bộ nhớ
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Data");

            // Headers
            ws.Cell(1, 1).Value = "Mã SKU (*)";
            ws.Cell(1, 2).Value = "Mã vạch Barcode";
            ws.Cell(1, 3).Value = "Tên sản phẩm (*)";
            ws.Cell(1, 4).Value = "Loại hàng";
            ws.Cell(1, 5).Value = "Tên Danh mục";
            ws.Cell(1, 6).Value = "Đơn vị tính";
            ws.Cell(1, 7).Value = "Giá vốn";
            ws.Cell(1, 8).Value = "Giá bán";

            // Dòng 1: Hợp lệ
            ws.Cell(2, 1).Value = "VALID-SKU-01";
            ws.Cell(2, 2).Value = "";
            ws.Cell(2, 3).Value = "Bút chì 2B";
            ws.Cell(2, 4).Value = "VanPhongPham";
            ws.Cell(2, 5).Value = "";
            ws.Cell(2, 6).Value = "Cây";
            ws.Cell(2, 7).Value = 3000;
            ws.Cell(2, 8).Value = 5000;

            // Dòng 2: Thiếu SKU
            ws.Cell(3, 1).Value = "";
            ws.Cell(3, 2).Value = "";
            ws.Cell(3, 3).Value = "Sách không có SKU";
            ws.Cell(3, 4).Value = "Sach";

            // Dòng 3: SKU trùng lặp với Dòng 1
            ws.Cell(4, 1).Value = "VALID-SKU-01"; // Trùng dòng 1
            ws.Cell(4, 2).Value = "";
            ws.Cell(4, 3).Value = "Bút chì trùng SKU";
            ws.Cell(4, 4).Value = "VanPhongPham";

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;

            // Act
            var result = await excelService.ImportProductsFromExcelAsync(ms, "Tester");

            // Assert
            Assert.Equal(3, result.TotalRows);
            Assert.Equal(1, result.SuccessCount); // Chỉ dòng 1 thành công
            Assert.Equal(2, result.ErrorCount);   // Dòng 2 và 3 bị bắt lỗi
            Assert.NotNull(result.ErrorReportFileBytes); // Đã sinh file báo cáo lỗi
            Assert.Contains(result.Errors, e => e.ColumnName == "Mã SKU" && e.ErrorMessage.Contains("bắt buộc"));
            Assert.Contains(result.Errors, e => e.ColumnName == "Mã SKU" && e.ErrorMessage.Contains("trùng lặp"));
        }
    }
}
