using ABCBookAPIService.DTOs.Catalog;

namespace ABCBookAPIService.Interfaces
{
    public interface IProductService
    {
        Task<PagedResult<ProductDto>> GetPagedProductsAsync(ProductFilterDto filter);
        Task<ProductDto?> GetProductByIdAsync(Guid id);
        Task<ProductDto?> GetProductByBarcodeOrSkuAsync(string code);
        Task<Guid> CreateProductAsync(ProductCreateUpdateDto dto, string operatorUserName);
        Task<bool> UpdateProductAsync(Guid id, ProductCreateUpdateDto dto, string operatorUserName);
        Task<bool> ToggleProductStatusAsync(Guid id, string operatorUserName);
        Task<string> GenerateUniqueBarcodeAsync();
    }

    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(Guid id);
        Task<Guid> CreateCategoryAsync(CategoryDto dto, string operatorUserName);
        Task<bool> UpdateCategoryAsync(Guid id, CategoryDto dto, string operatorUserName);
        Task<bool> DeleteCategoryAsync(Guid id, string operatorUserName);
    }

    public interface IProductExcelService
    {
        Task<byte[]> ExportProductsToExcelAsync(ProductFilterDto? filter = null);
        Task<byte[]> GenerateExcelImportTemplateAsync();
        Task<ExcelImportResultDto> ImportProductsFromExcelAsync(Stream fileStream, string operatorUserName);
    }
}
