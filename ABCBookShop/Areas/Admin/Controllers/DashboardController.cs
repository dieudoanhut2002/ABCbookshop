using ABCBookAPIService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCBookShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IStorefrontService _storefrontService;

        public DashboardController(IStorefrontService storefrontService)
        {
            _storefrontService = storefrontService;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Index()
        {
            var summary = await _storefrontService.GetChainDashboardSummaryAsync();
            return View(summary);
        }
    }
}
