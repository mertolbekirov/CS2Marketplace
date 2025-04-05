using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CS2Marketplace.Data;
using CS2Marketplace.Models;
using CS2Marketplace.Services.Interfaces;

namespace CS2Marketplace.Services
{
    public class TradeService : ITradeService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserService _userService;

        public TradeService(ApplicationDbContext dbContext, IUserService userService)
        {
            _dbContext = dbContext;
            _userService = userService;
        }

        public async Task<IEnumerable<Trade>> GetUserTradesAsync(string steamId)
        {
            var user = await _userService.GetUserBySteamIdAsync(steamId);
            if (user == null)
                return Enumerable.Empty<Trade>();

            return await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .Include(t => t.Item)
                .Where(t => t.SellerId == user.Id || t.BuyerId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Trade> GetTradeDetailsAsync(int tradeId, string steamId)
        {
            var user = await _userService.GetUserBySteamIdAsync(steamId);
            if (user == null)
                return null;

            var trade = await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .Include(t => t.Listing)
                .Include(t => t.Item)
                .FirstOrDefaultAsync(t => t.Id == tradeId);

            if (trade == null || (trade.SellerId != user.Id && trade.BuyerId != user.Id))
                return null;

            return trade;
        }

        public async Task<(bool success, string tradeLink, string message)> CreateTradeOfferAsync(int tradeId, string sellerSteamId)
        {
            var trade = await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .FirstOrDefaultAsync(t => t.Id == tradeId);

            if (trade == null)
                return (false, null, "Trade not found.");

            var seller = await _userService.GetUserBySteamIdAsync(sellerSteamId);
            if (seller == null || trade.SellerId != seller.Id)
                return (false, null, "Unauthorized.");

            if (string.IsNullOrEmpty(trade.Buyer.TradeLink))
                return (false, null, "The buyer has not set up their trade link. Please contact them.");

            trade.Status = TradeStatus.WaitingForSeller;
            trade.StatusMessage = "Waiting for seller to send trade offer...";
            await _dbContext.SaveChangesAsync();

            return (true, trade.Buyer.TradeLink, null);
        }

        public async Task<(bool success, string message)> MarkTradeAsSentAsync(int tradeId, string sellerSteamId)
        {
            var trade = await _dbContext.Trades
                .Include(t => t.Buyer)
                .FirstOrDefaultAsync(t => t.Id == tradeId);

            if (trade == null)
                return (false, "Trade not found.");

            var seller = await _userService.GetUserBySteamIdAsync(sellerSteamId);
            if (seller == null || trade.SellerId != seller.Id)
                return (false, "Unauthorized.");

            if (trade.Status != TradeStatus.WaitingForSeller)
                return (false, "This trade is no longer in a state where it can be marked as sent.");

            trade.Status = TradeStatus.WaitingForBuyerConfirmation;
            trade.OfferSentAt = DateTime.UtcNow;
            trade.BuyerResponseDeadline = DateTime.UtcNow.AddHours(24);
            trade.StatusMessage = "Waiting for buyer to confirm receipt...";

            await _dbContext.SaveChangesAsync();

            return (true, "Trade offer has been marked as sent. The buyer has 24 hours to confirm receipt.");
        }

        public async Task<(bool success, string message)> ConfirmTradeReceiptAsync(int tradeId, string buyerSteamId)
        {
            var trade = await _dbContext.Trades
                .Include(t => t.Seller)
                .FirstOrDefaultAsync(t => t.Id == tradeId);

            if (trade == null)
                return (false, "Trade not found.");

            var buyer = await _userService.GetUserBySteamIdAsync(buyerSteamId);
            if (buyer == null || trade.BuyerId != buyer.Id)
                return (false, "Unauthorized.");

            if (!trade.CanBuyerRespond)
                return (false, "This trade can no longer be confirmed. The confirmation period has expired or the trade is in an invalid state.");

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
                Description = $"Sale completed for trade #{trade.Id}",
                Status = WalletTransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                ReferenceId = trade.Id.ToString()
            };

            _dbContext.WalletTransactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            return (true, "Trade has been confirmed and completed successfully.");
        }

        public async Task<(bool success, string message)> DisputeTradeAsync(int tradeId, string buyerSteamId, string reason)
        {
            var trade = await _dbContext.Trades.FirstOrDefaultAsync(t => t.Id == tradeId);
            if (trade == null)
                return (false, "Trade not found.");

            var buyer = await _userService.GetUserBySteamIdAsync(buyerSteamId);
            if (buyer == null || trade.BuyerId != buyer.Id)
                return (false, "Unauthorized.");

            if (!trade.CanBuyerRespond)
                return (false, "This trade can no longer be disputed. The response period has expired or the trade is in an invalid state.");

            trade.Status = TradeStatus.Disputed;
            trade.DisputeReason = reason;
            trade.DisputedAt = DateTime.UtcNow;
            trade.StatusMessage = "Trade under review due to dispute";

            await _dbContext.SaveChangesAsync();

            return (true, "Trade has been marked as disputed. Our support team will review the case and contact both parties.");
        }
    }
} 