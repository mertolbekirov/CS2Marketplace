using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Services;
using CS2Marketplace.Filters;
using System.Threading.Tasks;
using CS2Marketplace.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using CS2Marketplace.Services.Interfaces;

namespace CS2Marketplace.Controllers
{
    [RequireAuthentication]
    public class MarketplaceController : Controller
    {
        private readonly IMarketplaceService _marketService;

        public MarketplaceController(IMarketplaceService marketService)
        {
            _marketService = marketService;
        }

        // GET: /Marketplace/LoadInventory
        public async Task<IActionResult> LoadInventory()
        {
            string currentUserSteamId = HttpContext.Session.GetString("SteamId")!;
            InventoryViewModel viewModel = await _marketService.LoadInventory(currentUserSteamId);
            return View(viewModel);
        }

        // POST: /Marketplace/CreateListing
        [HttpPost]
        [SellerEligibilityFilter]
        public async Task<IActionResult> CreateListing(string assetId, decimal price)
        {
            string currentUserSteamId = HttpContext.Session.GetString("SteamId")!;
            var (success, tempDataKey, tempDataMessage) = await _marketService.CreateListing(currentUserSteamId, assetId, price);
            if (!string.IsNullOrEmpty(tempDataKey))
                TempData[tempDataKey] = tempDataMessage;
            return success
                ? RedirectToAction("Index", "Marketplace")
                : RedirectToAction("LoadInventory", "Marketplace");
        }

        // GET: /Marketplace/Index
        public async Task<IActionResult> Index()
        {
            var listings = await _marketService.GetActiveListings();
            return View(listings);
        }

        // POST: /Marketplace/Purchase/{id}
        [HttpPost]
        public async Task<IActionResult> Purchase(int id)
        {
            string currentUserSteamId = HttpContext.Session.GetString("SteamId")!;
            var (success, tempDataKey, tempDataMessage, tradeId) = await _marketService.Purchase(currentUserSteamId, id);
            if (!string.IsNullOrEmpty(tempDataKey))
                TempData[tempDataKey] = tempDataMessage;
            return success
                ? RedirectToAction("Details", "Trade", new { id = tradeId })
                : RedirectToAction("Index", "Marketplace");
        }
    }
}
