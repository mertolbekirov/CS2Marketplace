using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Services;
using CS2Marketplace.Models;
using CS2Marketplace.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace CS2Marketplace.Controllers
{
    public class TradeController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public TradeController(
            ApplicationDbContext dbContext,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        // GET: /Trade
        public async Task<IActionResult> Index()
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamId))
                return RedirectToAction("SignIn", "Auth");

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
                return RedirectToAction("SignIn", "Auth");

            var trades = await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .Where(t => t.SellerId == user.Id || t.BuyerId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(trades);
        }

        // GET: /Trade/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamId))
                return RedirectToAction("SignIn", "Auth");

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
                return RedirectToAction("SignIn", "Auth");

            var trade = await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .Include(t => t.Listing)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trade == null)
                return NotFound();

            // Only allow seller and buyer to view trade details
            if (trade.SellerId != user.Id && trade.BuyerId != user.Id)
                return Forbid();

            return View(trade);
        }

        // POST: /Trade/CreateOffer
        [HttpPost]
        public async Task<IActionResult> CreateOffer(int tradeId)
        {
            var trade = await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .FirstOrDefaultAsync(t => t.Id == tradeId);

            if (trade == null)
                return NotFound();

            string steamId = HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamId))
                return RedirectToAction("SignIn", "Auth");

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
                return RedirectToAction("SignIn", "Auth");

            // Only the seller can send the trade offer
            if (trade.SellerId != user.Id)
                return Forbid();

            // Check if buyer has a trade link
            if (string.IsNullOrEmpty(trade.Buyer.TradeLink))
            {
                return Json(new { success = false, message = "The buyer has not set up their trade link. Please contact them." });
            }

            // Update trade status to indicate we're waiting for the seller to send the offer
            trade.Status = TradeStatus.WaitingForSeller;
            trade.StatusMessage = "Waiting for seller to send trade offer...";
            await _dbContext.SaveChangesAsync();

            // Return the trade link to be opened in a new window
            return Json(new { success = true, tradeLink = trade.Buyer.TradeLink });
        }

        // POST: /Trade/MarkAsSent/{id}
        [HttpPost]
        public async Task<IActionResult> MarkAsSent(int id)
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamId))
                return RedirectToAction("SignIn", "Auth");

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
                return RedirectToAction("SignIn", "Auth");

            var trade = await _dbContext.Trades
                .Include(t => t.Buyer)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trade == null)
                return NotFound();

            // Verify seller
            if (trade.SellerId != user.Id)
                return Forbid();

            if (trade.Status != TradeStatus.WaitingForSeller)
            {
                TempData["Error"] = "This trade is no longer in a state where it can be marked as sent.";
                return RedirectToAction(nameof(Details), new { id = trade.Id });
            }

            // Update trade status
            trade.Status = TradeStatus.WaitingForBuyerConfirmation;
            trade.OfferSentAt = DateTime.UtcNow;
            trade.BuyerResponseDeadline = DateTime.UtcNow.AddHours(24);
            trade.StatusMessage = "Waiting for buyer to confirm receipt...";

            await _dbContext.SaveChangesAsync();

            // TODO: Send email/notification to buyer

            TempData["Success"] = "Trade offer has been marked as sent. The buyer has 24 hours to confirm receipt.";
            return RedirectToAction(nameof(Details), new { id = trade.Id });
        }

        // POST: /Trade/ConfirmReceipt/{id}
        [HttpPost]
        public async Task<IActionResult> ConfirmReceipt(int id)
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamId))
                return RedirectToAction("SignIn", "Auth");

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
                return RedirectToAction("SignIn", "Auth");

            var trade = await _dbContext.Trades
                .Include(t => t.Seller)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trade == null)
                return NotFound();

            // Verify buyer
            if (trade.BuyerId != user.Id)
                return Forbid();

            if (!trade.CanBuyerRespond)
            {
                TempData["Error"] = "This trade can no longer be confirmed. The confirmation period has expired or the trade is in an invalid state.";
                return RedirectToAction(nameof(Details), new { id = trade.Id });
            }

            // Complete the trade
            trade.Status = TradeStatus.Completed;
            trade.CompletedAt = DateTime.UtcNow;
            trade.StatusMessage = "Trade completed successfully";

            // Release funds to seller
            var seller = trade.Seller;
            seller.Balance += trade.Amount;

            // Create wallet transaction for seller
            var transaction = new WalletTransaction
            {
                UserId = seller.Id,
                Amount = trade.Amount,
                Type = WalletTransactionType.Sale,
                Description = $"Sale completed: {trade.ItemName}",
                CreatedAt = DateTime.UtcNow,
                Status = WalletTransactionStatus.Completed,
                ReferenceId = trade.Id.ToString()
            };

            _dbContext.WalletTransactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            // TODO: Send email/notification to seller

            TempData["Success"] = "Trade has been confirmed as completed. The funds have been released to the seller.";
            return RedirectToAction(nameof(Details), new { id = trade.Id });
        }

        // POST: /Trade/DisputeTrade/{id}
        [HttpPost]
        public async Task<IActionResult> DisputeTrade(int id, string reason)
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamId))
                return RedirectToAction("SignIn", "Auth");

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
                return RedirectToAction("SignIn", "Auth");

            var trade = await _dbContext.Trades.FirstOrDefaultAsync(t => t.Id == id);

            if (trade == null)
                return NotFound();

            // Verify buyer
            if (trade.BuyerId != user.Id)
                return Forbid();

            if (!trade.CanBuyerRespond)
            {
                TempData["Error"] = "This trade can no longer be disputed. The response period has expired or the trade is in an invalid state.";
                return RedirectToAction(nameof(Details), new { id = trade.Id });
            }

            // Mark trade as disputed
            trade.Status = TradeStatus.Disputed;
            trade.DisputeReason = reason;
            trade.DisputedAt = DateTime.UtcNow;
            trade.StatusMessage = "Trade under review due to dispute";

            await _dbContext.SaveChangesAsync();

            // TODO: Send email/notification to admin and seller

            TempData["Success"] = "Trade has been marked as disputed. Our support team will review the case and contact both parties.";
            return RedirectToAction(nameof(Details), new { id = trade.Id });
        }
    }
}
