using ABCBook.Model.Entity;
using ABCBookAPIService.DTOs.Catalog;
using ABCBookAPIService.DTOs.Storefront;

namespace ABCBookAPIService.Interfaces
{
    public interface IStorefrontService
    {
        Task<List<ProductDto>> GetFeaturedProductsAsync(int count = 8);
        Task<List<ProductDto>> GetNewArrivalsAsync(int count = 8);
        Task<List<CategoryDto>> GetActiveCategoriesAsync();
        Task<ProductDto?> GetProductDetailsAsync(Guid id);
        Task<Dictionary<string, decimal>> GetStockAcrossStoresAsync(Guid productId);
        Task<Guid> PlaceOnlineOrderAsync(ShoppingCartDto cart, PublicCheckoutDto checkoutInfo);
        Task<Order?> TrackOrderAsync(string orderNumber, string phoneNumber);
        Task<ChainDashboardSummaryDto> GetChainDashboardSummaryAsync();
    }
}
