using ABCBook.Model;
using ABCBook.Model.Entity;
using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ABCBookAPIService.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ABCBookShopContext _context;

        public CategoryService(ABCBookShopContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    ParentId = c.ParentId,
                    ParentName = c.Parent != null ? c.Parent.Name : null,
                    Description = c.Description,
                    ProductsCount = c.Products.Count(p => !p.IsDeleted),
                    IsActive = c.IsActive
                })
                .ToListAsync();
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(Guid id)
        {
            var c = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (c == null) return null;

            return new CategoryDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                ParentId = c.ParentId,
                ParentName = c.Parent?.Name,
                Description = c.Description,
                ProductsCount = c.Products.Count(p => !p.IsDeleted),
                IsActive = c.IsActive
            };
        }

        public async Task<Guid> CreateCategoryAsync(CategoryDto dto, string operatorUserName)
        {
            var exists = await _context.Categories.AnyAsync(c => c.Code == dto.Code && !c.IsDeleted);
            if (exists)
            {
                throw new InvalidOperationException($"Mã danh mục '{dto.Code}' đã tồn tại trong hệ thống.");
            }

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Code = dto.Code.Trim().ToUpper(),
                Name = dto.Name.Trim(),
                ParentId = dto.ParentId,
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedBy = operatorUserName,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
            return category.Id;
        }

        public async Task<bool> UpdateCategoryAsync(Guid id, CategoryDto dto, string operatorUserName)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category == null) return false;

            category.Name = dto.Name.Trim();
            category.ParentId = dto.ParentId;
            category.Description = dto.Description;
            category.IsActive = dto.IsActive;
            category.UpdatedBy = operatorUserName;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(Guid id, string operatorUserName)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (category == null) return false;

            if (category.Products.Any(p => !p.IsDeleted))
            {
                throw new InvalidOperationException("Không thể xóa danh mục đang có sản phẩm kinh doanh. Vui lòng chuyển sản phẩm sang danh mục khác trước.");
            }

            category.IsDeleted = true;
            category.UpdatedBy = operatorUserName;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }

    public class ProductService : IProductService
    {
        private readonly ABCBookShopContext _context;

        public ProductService(ABCBookShopContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ProductDto>> GetPagedProductsAsync(ProductFilterDto filter)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.BookDetail)
                .Include(p => p.StockBalances)
                .Where(p => !p.IsDeleted);

            // Tìm kiếm từ khóa (Keyword)
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(p =>
                    p.Title.Contains(kw) ||
                    p.SKU.Contains(kw) ||
                    p.Barcode.Contains(kw) ||
                    (p.BookDetail != null && (
                        p.BookDetail.Author.Contains(kw) ||
                        (p.BookDetail.Publisher != null && p.BookDetail.Publisher.Contains(kw)) ||
                        (p.BookDetail.ISBN != null && p.BookDetail.ISBN.Contains(kw))
                    )));
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            if (filter.ProductType.HasValue)
            {
                query = query.Where(p => p.ProductType == filter.ProductType.Value);
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == filter.IsActive.Value);
            }

            if (filter.OnlyLowStock == true)
            {
                query = query.Where(p => p.StockBalances.Sum(sb => sb.Quantity) <= p.LowStockThreshold);
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.SellingPrice >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.SellingPrice <= filter.MaxPrice.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((filter.PageIndex - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    SKU = p.SKU,
                    Barcode = p.Barcode,
                    Title = p.Title,
                    Description = p.Description,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : "Chưa phân loại",
                    ProductType = p.ProductType,
                    IsBook = p.IsBook,
                    Unit = p.Unit,
                    CostPrice = p.CostPrice,
                    SellingPrice = p.SellingPrice,
                    VatRate = p.VatRate,
                    LowStockThreshold = p.LowStockThreshold,
                    ImageUrl = p.ImageUrl,
                    IsActive = p.IsActive,
                    TotalStockQuantity = p.StockBalances.Sum(sb => sb.Quantity),
                    BookDetail = p.BookDetail != null ? new BookDetailDto
                    {
                        ISBN = p.BookDetail.ISBN,
                        Author = p.BookDetail.Author,
                        Publisher = p.BookDetail.Publisher,
                        PublicationYear = p.BookDetail.PublicationYear,
                        PageCount = p.BookDetail.PageCount,
                        Language = p.BookDetail.Language,
                        CoverType = p.BookDetail.CoverType
                    } : null
                })
                .ToListAsync();

            return new PagedResult<ProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize
            };
        }

        public async Task<ProductDto?> GetProductByIdAsync(Guid id)
        {
            var p = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.BookDetail)
                .Include(p => p.StockBalances)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (p == null) return null;

            return new ProductDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Barcode = p.Barcode,
                Title = p.Title,
                Description = p.Description,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? "Chưa phân loại",
                ProductType = p.ProductType,
                IsBook = p.IsBook,
                Unit = p.Unit,
                CostPrice = p.CostPrice,
                SellingPrice = p.SellingPrice,
                VatRate = p.VatRate,
                LowStockThreshold = p.LowStockThreshold,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                TotalStockQuantity = p.StockBalances.Sum(sb => sb.Quantity),
                BookDetail = p.BookDetail != null ? new BookDetailDto
                {
                    ISBN = p.BookDetail.ISBN,
                    Author = p.BookDetail.Author,
                    Publisher = p.BookDetail.Publisher,
                    PublicationYear = p.BookDetail.PublicationYear,
                    PageCount = p.BookDetail.PageCount,
                    Language = p.BookDetail.Language,
                    CoverType = p.BookDetail.CoverType
                } : null
            };
        }

        public async Task<ProductDto?> GetProductByBarcodeOrSkuAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var cleanCode = code.Trim();

            var p = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.BookDetail)
                .Include(p => p.StockBalances)
                .FirstOrDefaultAsync(x => !x.IsDeleted && (x.Barcode == cleanCode || x.SKU == cleanCode || (x.BookDetail != null && x.BookDetail.ISBN == cleanCode)));

            if (p == null) return null;

            return new ProductDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Barcode = p.Barcode,
                Title = p.Title,
                Description = p.Description,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? "Chưa phân loại",
                ProductType = p.ProductType,
                IsBook = p.IsBook,
                Unit = p.Unit,
                CostPrice = p.CostPrice,
                SellingPrice = p.SellingPrice,
                VatRate = p.VatRate,
                LowStockThreshold = p.LowStockThreshold,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                TotalStockQuantity = p.StockBalances.Sum(sb => sb.Quantity),
                BookDetail = p.BookDetail != null ? new BookDetailDto
                {
                    ISBN = p.BookDetail.ISBN,
                    Author = p.BookDetail.Author,
                    Publisher = p.BookDetail.Publisher,
                    PublicationYear = p.BookDetail.PublicationYear,
                    PageCount = p.BookDetail.PageCount,
                    Language = p.BookDetail.Language,
                    CoverType = p.BookDetail.CoverType
                } : null
            };
        }

        public async Task<string> GenerateUniqueBarcodeAsync()
        {
            string barcode;
            bool exists;
            var prefix = $"ABC{DateTime.UtcNow:yyMM}"; // Ví dụ: ABC2610

            do
            {
                var randomNum = RandomNumberGenerator.GetInt32(100000, 999999);
                barcode = $"{prefix}{randomNum}";
                exists = await _context.Products.AnyAsync(p => p.Barcode == barcode && !p.IsDeleted);
            } while (exists);

            return barcode;
        }

        public async Task<Guid> CreateProductAsync(ProductCreateUpdateDto dto, string operatorUserName)
        {
            var skuClean = dto.SKU.Trim().ToUpper();
            var skuExists = await _context.Products.AnyAsync(p => p.SKU == skuClean && !p.IsDeleted);
            if (skuExists)
            {
                throw new InvalidOperationException($"Mã SKU '{skuClean}' đã tồn tại trong hệ thống.");
            }

            string barcode = dto.Barcode?.Trim() ?? string.Empty;
            if (dto.AutoGenerateBarcode || string.IsNullOrWhiteSpace(barcode))
            {
                barcode = await GenerateUniqueBarcodeAsync();
            }
            else
            {
                var barcodeExists = await _context.Products.AnyAsync(p => p.Barcode == barcode && !p.IsDeleted);
                if (barcodeExists)
                {
                    throw new InvalidOperationException($"Mã vạch '{barcode}' đã tồn tại cho một sản phẩm khác.");
                }
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                SKU = skuClean,
                Barcode = barcode,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                CategoryId = dto.CategoryId,
                ProductType = dto.ProductType,
                IsBook = dto.ProductType == ProductType.Book,
                Unit = dto.Unit?.Trim() ?? (dto.ProductType == ProductType.Book ? "Cuốn" : "Cái"),
                CostPrice = dto.CostPrice,
                SellingPrice = dto.SellingPrice,
                VatRate = dto.VatRate,
                LowStockThreshold = dto.LowStockThreshold,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive,
                CreatedBy = operatorUserName,
                CreatedAt = DateTime.UtcNow
            };

            if (product.IsBook)
            {
                product.BookDetail = new BookDetail
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ISBN = dto.ISBN?.Trim(),
                    Author = !string.IsNullOrWhiteSpace(dto.Author) ? dto.Author.Trim() : "Đang cập nhật",
                    Publisher = dto.Publisher?.Trim(),
                    PublicationYear = dto.PublicationYear,
                    PageCount = dto.PageCount,
                    Language = dto.Language?.Trim() ?? "Tiếng Việt",
                    CoverType = dto.CoverType?.Trim() ?? "Bìa mềm"
                };
            }

            await _context.Products.AddAsync(product);

            // Ghi nhận AuditLog
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserName = operatorUserName,
                Action = "CREATE_PRODUCT",
                EntityName = nameof(Product),
                EntityId = product.Id.ToString(),
                NewValues = $"SKU: {product.SKU}, Title: {product.Title}, SellingPrice: {product.SellingPrice}",
                Timestamp = DateTime.UtcNow
            };
            await _context.AuditLogs.AddAsync(auditLog);

            await _context.SaveChangesAsync();
            return product.Id;
        }

        public async Task<bool> UpdateProductAsync(Guid id, ProductCreateUpdateDto dto, string operatorUserName)
        {
            var product = await _context.Products
                .Include(p => p.BookDetail)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return false;

            var skuClean = dto.SKU.Trim().ToUpper();
            if (product.SKU != skuClean)
            {
                var skuExists = await _context.Products.AnyAsync(p => p.SKU == skuClean && p.Id != id && !p.IsDeleted);
                if (skuExists)
                {
                    throw new InvalidOperationException($"Mã SKU '{skuClean}' đã được sử dụng bởi sản phẩm khác.");
                }
                product.SKU = skuClean;
            }

            var barcodeClean = dto.Barcode?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(barcodeClean) && product.Barcode != barcodeClean)
            {
                var barcodeExists = await _context.Products.AnyAsync(p => p.Barcode == barcodeClean && p.Id != id && !p.IsDeleted);
                if (barcodeExists)
                {
                    throw new InvalidOperationException($"Mã vạch '{barcodeClean}' đã tồn tại cho một sản phẩm khác.");
                }
                product.Barcode = barcodeClean;
            }

            var oldValues = $"Price: {product.SellingPrice}, Title: {product.Title}";

            product.Title = dto.Title.Trim();
            product.Description = dto.Description?.Trim() ?? string.Empty;
            product.CategoryId = dto.CategoryId;
            product.ProductType = dto.ProductType;
            product.IsBook = dto.ProductType == ProductType.Book;
            product.Unit = dto.Unit?.Trim() ?? product.Unit;
            product.CostPrice = dto.CostPrice;
            product.SellingPrice = dto.SellingPrice;
            product.VatRate = dto.VatRate;
            product.LowStockThreshold = dto.LowStockThreshold;
            product.ImageUrl = dto.ImageUrl ?? product.ImageUrl;
            product.IsActive = dto.IsActive;
            product.UpdatedBy = operatorUserName;
            product.UpdatedAt = DateTime.UtcNow;

            if (product.IsBook)
            {
                if (product.BookDetail == null)
                {
                    product.BookDetail = new BookDetail { Id = Guid.NewGuid(), ProductId = product.Id };
                }
                product.BookDetail.ISBN = dto.ISBN?.Trim();
                product.BookDetail.Author = !string.IsNullOrWhiteSpace(dto.Author) ? dto.Author.Trim() : product.BookDetail.Author;
                product.BookDetail.Publisher = dto.Publisher?.Trim();
                product.BookDetail.PublicationYear = dto.PublicationYear;
                product.BookDetail.PageCount = dto.PageCount;
                product.BookDetail.Language = dto.Language?.Trim() ?? "Tiếng Việt";
                product.BookDetail.CoverType = dto.CoverType?.Trim() ?? "Bìa mềm";
            }

            // Ghi vết sửa giá / thông tin
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserName = operatorUserName,
                Action = "UPDATE_PRODUCT",
                EntityName = nameof(Product),
                EntityId = product.Id.ToString(),
                OldValues = oldValues,
                NewValues = $"Price: {product.SellingPrice}, Title: {product.Title}",
                Timestamp = DateTime.UtcNow
            };
            await _context.AuditLogs.AddAsync(auditLog);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleProductStatusAsync(Guid id, string operatorUserName)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (product == null) return false;

            product.IsActive = !product.IsActive;
            product.UpdatedBy = operatorUserName;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
