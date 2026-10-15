using ABCBook.Model;
using ABCBookAPIService.DTOs.Storefront;
using ABCBookAPIService.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ABCBookShop.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "ABCBookShop_ShoppingCart";
        private readonly IStorefrontService _storefrontService;
        private readonly IProductService _productService;
        private readonly ABCBookShopContext _context;

        public CartController(
            IStorefrontService storefrontService,
            IProductService productService,
            ABCBookShopContext context)
        {
            _storefrontService = storefrontService;
            _productService = productService;
            _context = context;
        }

        private ShoppingCartDto GetCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return new ShoppingCartDto();
            }
            return JsonSerializer.Deserialize<ShoppingCartDto>(json) ?? new ShoppingCartDto();
        }

        private void SaveCart(ShoppingCartDto cart)
        {
            var json = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(CartSessionKey, json);
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(Guid productId, decimal quantity = 1)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null) return NotFound();

            var cart = GetCart();
            var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                cart.Items.Add(new CartItemDto
                {
                    ProductId = product.Id,
                    SKU = product.SKU,
                    Title = product.Title,
                    ImageUrl = product.ImageUrl,
                    UnitPrice = product.SellingPrice,
                    Quantity = quantity
                });
            }

            SaveCart(cart);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, totalItems = cart.TotalItemCount, message = $"Đã thêm '{product.Title}' vào giỏ hàng." });
            }

            TempData["SuccessMessage"] = $"Đã thêm '{product.Title}' vào giỏ hàng thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/UpdateQty
        [HttpPost]
        public IActionResult UpdateQty(Guid productId, decimal quantity)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                SaveCart(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        public IActionResult RemoveItem(Guid productId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                cart.Items.Remove(item);
                SaveCart(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Cart/Checkout
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống. Vui lòng chọn sách trước khi đặt hàng.";
                return RedirectToAction("Books", "Home");
            }

            ViewBag.Cart = cart;
            return View(new PublicCheckoutDto());
        }

        // POST: /Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(PublicCheckoutDto dto)
        {
            var cart = GetCart();
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Books", "Home");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Cart = cart;
                return View(dto);
            }

            try
            {
                var orderId = await _storefrontService.PlaceOnlineOrderAsync(cart, dto);

                // Xóa giỏ hàng sau khi đặt thành công
                HttpContext.Session.Remove(CartSessionKey);

                return RedirectToAction(nameof(OrderConfirmation), new { id = orderId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Cart = cart;
                return View(dto);
            }
        }

        // GET: /Cart/OrderConfirmation/5
        public async Task<IActionResult> OrderConfirmation(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.Store)
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (order == null) return NotFound();
            return View(order);
        }
    }
}
