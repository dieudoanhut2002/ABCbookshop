using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace ABCBookAPIService.DTOs.Catalog
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; } = "Chưa phân loại";
        public ProductType ProductType { get; set; }
        public bool IsBook { get; set; }
        public string Unit { get; set; } = "Cuốn";
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal VatRate { get; set; }
        public decimal LowStockThreshold { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public decimal TotalStockQuantity { get; set; }

        // Thuộc tính sách
        public BookDetailDto? BookDetail { get; set; }
    }

    public class BookDetailDto
    {
        public string? ISBN { get; set; }
        public string Author { get; set; } = string.Empty;
        public string? Publisher { get; set; }
        public int? PublicationYear { get; set; }
        public int? PageCount { get; set; }
        public string? Language { get; set; } = "Tiếng Việt";
        public string? CoverType { get; set; } = "Bìa mềm";
    }

    public class ProductCreateUpdateDto
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Mã SKU không được để trống")]
        [MaxLength(50, ErrorMessage = "Mã SKU tối đa 50 ký tự")]
        public string SKU { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Mã vạch tối đa 50 ký tự")]
        public string? Barcode { get; set; }

        public bool AutoGenerateBarcode { get; set; } = false;

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [MaxLength(250, ErrorMessage = "Tên sản phẩm tối đa 250 ký tự")]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Guid? CategoryId { get; set; }

        public ProductType ProductType { get; set; } = ProductType.Book;

        [Required(ErrorMessage = "Đơn vị tính không được để trống")]
        public string Unit { get; set; } = "Cuốn";

        [Range(0, 1000000000, ErrorMessage = "Giá vốn phải từ 0 trở lên")]
        public decimal CostPrice { get; set; } = 0;

        [Range(0, 1000000000, ErrorMessage = "Giá bán phải từ 0 trở lên")]
        public decimal SellingPrice { get; set; } = 0;

        [Range(0, 100, ErrorMessage = "Thuế VAT từ 0% đến 100%")]
        public decimal VatRate { get; set; } = 8;

        [Range(0, 100000, ErrorMessage = "Ngưỡng cảnh báo tồn kho tối thiểu từ 0")]
        public decimal LowStockThreshold { get; set; } = 5;

        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;

        // Chi tiết sách
        public string? ISBN { get; set; }
        public string? Author { get; set; }
        public string? Publisher { get; set; }
        public int? PublicationYear { get; set; }
        public int? PageCount { get; set; }
        public string? Language { get; set; } = "Tiếng Việt";
        public string? CoverType { get; set; } = "Bìa mềm";
    }

    public class ProductFilterDto
    {
        public string? Keyword { get; set; }
        public Guid? CategoryId { get; set; }
        public ProductType? ProductType { get; set; }
        public bool? IsActive { get; set; }
        public bool? OnlyLowStock { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public string? ParentName { get; set; }
        public string Description { get; set; } = string.Empty;
        public int ProductsCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class ExcelImportResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<ExcelRowErrorDto> Errors { get; set; } = new List<ExcelRowErrorDto>();
        public byte[]? ErrorReportFileBytes { get; set; }
        public string? ErrorReportFileName { get; set; }
    }

    public class ExcelRowErrorDto
    {
        public int RowIndex { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }
}
