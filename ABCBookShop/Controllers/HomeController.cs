using ABCBook.Model.Enum;
using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ABCBookShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStorefrontService _storefrontService;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public HomeController(
            IStorefrontService storefrontService,
            IProductService productService,
            ICategoryService categoryService)
        {
            _storefrontService = storefrontService;
            _productService = productService;
            _categoryService = categoryService;
        }

        // GET: / (Trang chủ Storefront)
        public async Task<IActionResult> Index()
        {
            ViewBag.FeaturedProducts = await _storefrontService.GetFeaturedProductsAsync(8);
            ViewBag.NewArrivals = await _storefrontService.GetNewArrivalsAsync(8);
            ViewBag.Categories = await _storefrontService.GetActiveCategoriesAsync();
            return View();
        }

        // GET: /Home/Books (Danh mục sách & Tìm kiếm)
        public async Task<IActionResult> Books(
            Guid? categoryId,
            string? keyword,
            decimal? minPrice,
            decimal? maxPrice,
            string? sort,
            int pageIndex = 1)
        {
            ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentKeyword = keyword;
            ViewBag.CurrentSort = sort;

            var filter = new ProductFilterDto
            {
                CategoryId = categoryId,
                Keyword = keyword,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                IsActive = true,
                PageIndex = pageIndex,
                PageSize = 12
            };

            var pagedProducts = await _productService.GetPagedProductsAsync(filter);
            return View(pagedProducts);
        }

        // GET: /Home/Details/5 (Chi tiết sách & Tồn kho các chi nhánh)
        public async Task<IActionResult> Details(Guid id)
        {
            var product = await _storefrontService.GetProductDetailsAsync(id);
            if (product == null) return NotFound();

            ViewBag.StockAcrossStores = await _storefrontService.GetStockAcrossStoresAsync(id);
            return View(product);
        }

        // GET: /Home/OrderTracking (Tra cứu đơn hàng)
        public async Task<IActionResult> OrderTracking(string? orderNumber, string? phone)
        {
            ViewBag.OrderNumber = orderNumber;
            ViewBag.Phone = phone;

            if (!string.IsNullOrWhiteSpace(orderNumber) && !string.IsNullOrWhiteSpace(phone))
            {
                var order = await _storefrontService.TrackOrderAsync(orderNumber, phone);
                ViewBag.Order = order;
                ViewBag.Searched = true;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
