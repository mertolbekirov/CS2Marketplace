using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CS2Marketplace.Data;
using CS2Marketplace.Models;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace CS2Marketplace.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public AdminController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: /Admin/Disputes
        public async Task<IActionResult> Disputes()
        {
            var disputes = await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .Where(t => t.Status == TradeStatus.Disputed)
                .OrderByDescending(t => t.DisputedAt)
                .ToListAsync();

            return View(disputes);
        }

        // GET: /Admin/DisputeDetails/{id}
        public async Task<IActionResult> DisputeDetails(int id)
        {
            var trade = await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .Include(t => t.Listing)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trade == null)
                return NotFound();

            if (trade.Status != TradeStatus.Disputed)
                return RedirectToAction(nameof(Disputes));

            return View(trade);
        }

        // POST: /Admin/ResolveDispute/{id}
        [HttpPost]
        public async Task<IActionResult> ResolveDispute(int id, bool refundBuyer, string resolution, string adminNotes)
        {
            var trade = await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trade == null)
                return NotFound();

            if (trade.Status != TradeStatus.Disputed)
            {
                TempData["Error"] = "This trade is not currently disputed.";
                return RedirectToAction(nameof(Disputes));
            }

            // Update trade status
            trade.Status = TradeStatus.DisputeResolved;
            trade.DisputeResolution = resolution;
            trade.AdminNotes = adminNotes;
            trade.ResolvedAt = DateTime.UtcNow;

            if (refundBuyer)
            {
                // Refund the buyer
                trade.IsRefunded = true;
                trade.RefundedAt = DateTime.UtcNow;
                trade.StatusMessage = "Trade disputed - Buyer refunded";

                var buyer = trade.Buyer;
                buyer.Balance += trade.Amount;

                // Create refund transaction
                var refundTransaction = new WalletTransaction
                {
                    UserId = buyer.Id,
                    Amount = trade.Amount,
                    Type = WalletTransactionType.Refund,
                    Description = $"Refund for disputed trade #{trade.Id}",
                    CreatedAt = DateTime.UtcNow,
                    Status = WalletTransactionStatus.Completed,
                    ReferenceId = trade.Id.ToString()
                };

                _dbContext.WalletTransactions.Add(refundTransaction);
            }
            else
            {
                // Release funds to seller
                trade.StatusMessage = "Trade disputed - Resolved in favor of seller";
                
                var seller = trade.Seller;
                seller.Balance += trade.Amount;

                // Create transaction for seller
                var transaction = new WalletTransaction
                {
                    UserId = seller.Id,
                    Amount = trade.Amount,
                    Type = WalletTransactionType.Sale,
                    Description = $"Funds released for disputed trade #{trade.Id}",
                    CreatedAt = DateTime.UtcNow,
                    Status = WalletTransactionStatus.Completed,
                    ReferenceId = trade.Id.ToString()
                };

                _dbContext.WalletTransactions.Add(transaction);
            }

            await _dbContext.SaveChangesAsync();

            // TODO: Send email notifications to both parties about the resolution

            TempData["Success"] = "Dispute has been resolved successfully.";
            return RedirectToAction(nameof(Disputes));
        }

        // POST: /Admin/ProcessExpiredTrades
        [HttpPost]
        public async Task<IActionResult> ProcessExpiredTrades()
        {
            var expiredTrades = await _dbContext.Trades
                .Include(t => t.Seller)
                .Where(t => t.Status == TradeStatus.WaitingForBuyerConfirmation
                           && t.BuyerResponseDeadline < DateTime.UtcNow)
                .ToListAsync();

            foreach (var trade in expiredTrades)
            {
                trade.Status = TradeStatus.Expired;
                trade.CompletedAt = DateTime.UtcNow;
                trade.StatusMessage = "Automatically completed due to buyer inaction";

                // Release funds to seller
                trade.Seller.Balance += trade.Amount;

                var transaction = new WalletTransaction
                {
                    UserId = trade.Seller.Id,
                    Amount = trade.Amount,
                    Type = WalletTransactionType.Sale,
                    Description = $"Sale auto-completed: {trade.ItemName}",
                    CreatedAt = DateTime.UtcNow,
                    Status = WalletTransactionStatus.Completed,
                    ReferenceId = trade.Id.ToString()
                };

                _dbContext.WalletTransactions.Add(transaction);
            }

            await _dbContext.SaveChangesAsync();

            TempData["Success"] = $"Processed {expiredTrades.Count} expired trades.";
            return RedirectToAction("Index", "Admin");
        }
    }
} 