using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CS2Marketplace.Data;
using CS2Marketplace.Models;
using CS2Marketplace.Services.Interfaces;
using System.Linq;

namespace CS2Marketplace.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserService _userService;

        public AdminService(ApplicationDbContext dbContext, IUserService userService)
        {
            _dbContext = dbContext;
            _userService = userService;
        }

        public async Task<IEnumerable<Trade>> GetDisputedTradesAsync()
        {
            return await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .Where(t => t.Status == TradeStatus.Disputed)
                .OrderByDescending(t => t.DisputedAt)
                .ToListAsync();
        }

        public async Task<Trade> GetDisputeDetailsAsync(int tradeId)
        {
            var trade = await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .Include(t => t.Listing)
                .FirstOrDefaultAsync(t => t.Id == tradeId);

            if (trade == null || trade.Status != TradeStatus.Disputed)
                return null;

            return trade;
        }

        public async Task<(bool success, string message)> ResolveDisputeAsync(int tradeId, bool refundBuyer, string resolution, string adminNotes)
        {
            var trade = await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .FirstOrDefaultAsync(t => t.Id == tradeId);

            if (trade == null)
                return (false, "Trade not found.");

            if (trade.Status != TradeStatus.Disputed)
                return (false, "Only disputed trades can be resolved.");

            // Update trade status and notes
            trade.Status = TradeStatus.DisputeResolved;
            trade.StatusMessage = $"Dispute resolved by admin: {resolution}";
            trade.AdminNotes = adminNotes;
            trade.DisputeResolution = resolution;
            trade.ResolvedAt = DateTime.UtcNow;

            if (refundBuyer)
            {
                // Refund to buyer's balance
                var buyer = trade.Buyer;
                buyer.Balance += trade.Amount;

                // Create refund transaction for buyer
                var refundTransaction = new WalletTransaction
                {
                    UserId = buyer.Id,
                    Amount = trade.Amount,
                    Type = WalletTransactionType.Refund,
                    Description = $"Refund for disputed trade #{trade.Id}",
                    Status = WalletTransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    ReferenceId = trade.Id.ToString()
                };

                _dbContext.WalletTransactions.Add(refundTransaction);
            }
            else
            {
                // Release funds to seller
                var seller = trade.Seller;
                seller.Balance += trade.Amount;

                // Create transaction for seller
                var transaction = new WalletTransaction
                {
                    UserId = seller.Id,
                    Amount = trade.Amount,
                    Type = WalletTransactionType.Sale,
                    Description = $"Funds released for disputed trade #{trade.Id}",
                    Status = WalletTransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    ReferenceId = trade.Id.ToString()
                };

                _dbContext.WalletTransactions.Add(transaction);
            }

            await _dbContext.SaveChangesAsync();

            return (true, "Dispute has been resolved successfully.");
        }

        public async Task<(bool success, string message)> ProcessExpiredTradesAsync()
        {
            var expiredTrades = await _dbContext.Trades
                .Include(t => t.Seller)
                .Where(t => t.Status == TradeStatus.WaitingForBuyerConfirmation
                       && t.BuyerResponseDeadline < DateTime.UtcNow)
                .ToListAsync();

            if (!expiredTrades.Any())
                return (false, "No expired trades found to process.");

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
                    Description = $"Sale auto-completed for trade #{trade.Id}",
                    CreatedAt = DateTime.UtcNow,
                    Status = WalletTransactionStatus.Completed,
                    ReferenceId = trade.Id.ToString()
                };

                _dbContext.WalletTransactions.Add(transaction);
            }

            await _dbContext.SaveChangesAsync();

            return (true, $"Processed {expiredTrades.Count} expired trades.");
        }

        public async Task<(bool success, string message)> MakeUserAdminAsync(string steamId)
        {
            var user = await _userService.GetUserBySteamIdAsync(steamId);
            if (user == null)
                return (false, "User not found.");

            user.IsAdmin = true;
            await _dbContext.SaveChangesAsync();

            return (true, $"User {user.Username} is now an admin.");
        }
    }
} 