using ABCBook.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace ABCBookAPIService.DTOs.Storefront
{
    public class CartItemDto
    {
        public Guid ProductId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal LineTotal => Quantity * UnitPrice;
    }

    public class ShoppingCartDto
    {
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public decimal SubTotal => Items.Sum(i => i.LineTotal);
        public decimal DiscountAmount { get; set; } = 0;
        public decimal ShippingFee { get; set; } = 0; // Miễn phí vận chuyển cho đơn >= 250,000đ
        public decimal TotalAmount => Math.Max(0, SubTotal - DiscountAmount + ShippingFee);
        public int TotalItemCount => (int)Items.Sum(i => i.Quantity);
    }

    public class PublicCheckoutDto
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        [MaxLength(300)]
        public string ShippingAddress { get; set; } = string.Empty;

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.VietQR;

        [MaxLength(500)]
        public string? Note { get; set; }

        public string? PromoCode { get; set; }
    }

    public class ChainDashboardSummaryDto
    {
        public decimal TodayRevenue { get; set; }
        public int TodayOrdersCount { get; set; }
        public decimal MonthRevenue { get; set; }
        public int MonthOrdersCount { get; set; }
        public int LowStockAlertCount { get; set; }
        public decimal TotalInventoryValue { get; set; }

        public List<StorePerformanceDto> StorePerformances { get; set; } = new List<StorePerformanceDto>();
        public List<TopSellingProductDto> TopSellingProducts { get; set; } = new List<TopSellingProductDto>();
        public List<RecentOrderDto> RecentOrders { get; set; } = new List<RecentOrderDto>();
    }

    public class StorePerformanceDto
    {
        public Guid StoreId { get; set; }
        public string StoreCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrdersCount { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }

    public class TopSellingProductDto
    {
        public Guid ProductId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RecentOrderDto
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public OrderSource Source { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
