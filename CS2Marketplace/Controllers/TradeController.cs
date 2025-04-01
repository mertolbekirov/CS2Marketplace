using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Services;
using CS2Marketplace.Models;
using CS2Marketplace.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CS2Marketplace.Controllers
{
    public class TradeController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly SteamTradeService _tradeService;

        public TradeController(
            ApplicationDbContext dbContext,
            SteamTradeService tradeService)
        {
            _dbContext = dbContext;
            _tradeService = tradeService;
        }

        // GET: /Trade
        public async Task<IActionResult> Index()
        {
            string steamIdStr = HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamIdStr))
                return RedirectToAction("SignIn", "Auth");

            int steamId = int.Parse(steamIdStr);
            var trades = await _dbContext.Trades
                .Include(t => t.Buyer)
                .Include(t => t.Seller)
                .Where(t => t.BuyerId == steamId || t.SellerId == steamId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(trades);
        }

        // GET: /Trade/Configure
        public IActionResult Configure()
        {
            ViewBag.ApiKeyUrl = _tradeService.GetSteamApiKeyUrl();
            return View();
        }

        // POST: /Trade/Configure
        [HttpPost]
        public async Task<IActionResult> Configure(string apiKey)
        {
            string steamIdStr = HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamIdStr))
                return RedirectToAction("SignIn", "Auth");

            if (await _tradeService.ValidateApiKey(apiKey))
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamIdStr);
                if (user != null)
                {
                    user.SteamApiKey = apiKey;
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction("Index", "Trade");
                }
            }

            ModelState.AddModelError("", "Invalid API key. Please check and try again.");
            ViewBag.ApiKeyUrl = _tradeService.GetSteamApiKeyUrl();
            return View();
        }

        // POST: /Trade/CreateOffer
        [HttpPost]
        public async Task<IActionResult> CreateOffer(int tradeId)
        {
            var trade = await _dbContext.Trades.FindAsync(tradeId);
            if (trade == null)
                return NotFound();

            string steamIdStr = HttpContext.Session.GetString("SteamId");
            int steamId = int.Parse(steamIdStr);
            if (trade.SellerId != steamId)
                return Forbid();

            if (await _tradeService.CreateTradeOffer(trade))
                return RedirectToAction("Index");

            TempData["Error"] = "Failed to create trade offer. Please try again.";
            return RedirectToAction("Index");
        }

        // POST: /Trade/Cancel
        [HttpPost]
        public async Task<IActionResult> Cancel(int tradeId)
        {
            var trade = await _dbContext.Trades.FindAsync(tradeId);
            if (trade == null)
                return NotFound();

            string steamIdStr = HttpContext.Session.GetString("SteamId");
            int steamId = int.Parse(steamIdStr);
            if (trade.BuyerId != steamId && trade.SellerId != steamId)
                return Forbid();

            trade.Status = TradeStatus.Cancelled;
            trade.StatusMessage = $"Cancelled by {(trade.BuyerId == steamId ? "buyer" : "seller")}";
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
