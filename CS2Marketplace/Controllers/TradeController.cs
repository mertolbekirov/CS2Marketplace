using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Services.Interfaces;
using CS2Marketplace.Filters;
using CS2Marketplace.Models;
using CS2Marketplace.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace CS2Marketplace.Controllers
{
    [RequireAuthentication]
    public class TradeController : Controller
    {
        private readonly ITradeService _tradeService;

        public TradeController(ITradeService tradeService)
        {
            _tradeService = tradeService;
        }

        // GET: /Trade
        public async Task<IActionResult> Index()
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var trades = await _tradeService.GetUserTradesAsync(steamId);
            return View(trades);
        }

        // GET: /Trade/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var trade = await _tradeService.GetTradeDetailsAsync(id, steamId);
            
            if (trade == null)
                return NotFound();

            return View(trade);
        }

        // POST: /Trade/CreateOffer
        [HttpPost]
        public async Task<IActionResult> CreateOffer(int tradeId)
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var (success, tradeLink, message) = await _tradeService.CreateTradeOfferAsync(tradeId, steamId);

            return Json(new { success, tradeLink, message });
        }

        // POST: /Trade/MarkAsSent/{id}
        [HttpPost]
        public async Task<IActionResult> MarkAsSent(int id)
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var (success, message) = await _tradeService.MarkTradeAsSentAsync(id, steamId);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Trade/ConfirmReceipt/{id}
        [HttpPost]
        public async Task<IActionResult> ConfirmReceipt(int id)
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var (success, message) = await _tradeService.ConfirmTradeReceiptAsync(id, steamId);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Trade/DisputeTrade/{id}
        [HttpPost]
        public async Task<IActionResult> DisputeTrade(int id, string reason)
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var (success, message) = await _tradeService.DisputeTradeAsync(id, steamId, reason);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
