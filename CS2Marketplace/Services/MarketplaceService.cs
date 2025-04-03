using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CS2Marketplace.Data;
using CS2Marketplace.Models;
using CS2Marketplace.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using CS2Marketplace.Services.Interfaces;

namespace CS2Marketplace.Services
{
    public class MarketplaceService : IMarketplaceService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly SteamApiService _steamApiService;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly IUserService _userService;

        public MarketplaceService(ApplicationDbContext dbContext, SteamApiService steamApiService, IConfiguration configuration, IMemoryCache cache, IUserService userService)
        {
            _dbContext = dbContext;
            _steamApiService = steamApiService;
            _configuration = configuration;
            _cache = cache;
            _userService = userService;
        }

        public async Task<InventoryViewModel> LoadInventory(string steamId)
        {
            var user = await _userService.GetUserBySteamIdAsync(steamId);

            if (user == null)
            {
                return new InventoryViewModel();
            }

            string appId = _configuration["Steam:AppId"] ?? "730";
            string contextId = _configuration["Steam:ContextId"] ?? "2";
            string appApiKey = _configuration["Steam:ApiKey"];
            if (string.IsNullOrEmpty(appApiKey))
            {
                return new InventoryViewModel();
            }

            string apiKeyToUse = !string.IsNullOrEmpty(user.SteamApiKey) ? user.SteamApiKey : appApiKey;

            var inventory = await _steamApiService.GetPlayerInventoryAsync(steamId, appId, contextId, apiKeyToUse)
                           ?? new List<InventoryItem>();

            var activeListingIds = await _dbContext.MarketplaceListings
                .Where(l => l.SellerId == user.Id && l.ListingStatus == ListingStatus.Active)
                .Select(l => l.UniqueAssetId)
                .ToListAsync();

            return new InventoryViewModel
            {
                Items = inventory,
                IsOwnInventory = true,
                HasApiKey = !string.IsNullOrEmpty(user.SteamApiKey),
                TargetUsername = user.Username,
                ActiveListingIds = activeListingIds
            };
        }

        public async Task<(bool success, string tempDataKey, string tempDataMessage)> CreateListing(string steamId, string assetId, decimal price)
        {
            var user = await _userService.GetUserBySteamIdAsync(steamId);
            if (user == null)
                return (false, "Error", "User not found.");

            if (string.IsNullOrEmpty(user.SteamApiKey))
                return (false, "Error", "You need to set up your Steam API Key in your profile before you can list items for sale.");

            bool alreadyListed = await _dbContext.MarketplaceListings.AnyAsync(l =>
                l.SellerId == user.Id &&
                l.UniqueAssetId == assetId &&
                l.ListingStatus == ListingStatus.Active);

            if (alreadyListed)
                return (false, "Error", "This item is already listed for sale.");

            string appId = _configuration["Steam:AppId"] ?? "730";
            string contextId = _configuration["Steam:ContextId"] ?? "2";
            var inventory = await _steamApiService.GetPlayerInventoryAsync(steamId, appId, contextId, user.SteamApiKey);

            var invItem = inventory?.FirstOrDefault(i => i.AssetId == assetId);
            if (invItem == null)
                return (false, "Error", "Item not found in inventory.");

            var item = await _dbContext.Items.FirstOrDefaultAsync(i => i.ExternalItemId == assetId);
            if (item == null)
            {
                item = new Item
                {
                    ExternalItemId = assetId,
                    Name = invItem.Name,
                    Description = string.Empty,
                    ImageUrl = invItem.ImageUrl,
                    Rarity = string.Empty
                };
                _dbContext.Items.Add(item);
                await _dbContext.SaveChangesAsync();
            }

            var listing = new MarketplaceListing
            {
                SellerId = user.Id,
                ItemId = item.Id,
                UniqueAssetId = assetId,
                Price = price,
                ListingStatus = ListingStatus.Active,
                ListedAt = DateTime.UtcNow,
                FloatValue = invItem.FloatValue,
                PatternIndex = invItem.PatternIndex
            };

            _dbContext.MarketplaceListings.Add(listing);
            await _dbContext.SaveChangesAsync();
            return (true, "Success", "Listing created successfully.");
        }

        public async Task<List<MarketplaceListing>> GetActiveListings()
        {
            return await _dbContext.MarketplaceListings
                .Include(l => l.Item)
                .Include(l => l.Seller)
                .Where(l => l.ListingStatus == ListingStatus.Active)
                .ToListAsync();
        }

        public async Task<(bool success, string tempDataKey, string tempDataMessage, int tradeId)> Purchase(string steamId, int listingId)
        {
            var user = await _userService.GetUserBySteamIdAsync(steamId);
            if (user == null)
                return (false, "Error", "User not found.", 0);

            if (string.IsNullOrEmpty(user.TradeLink))
                return (false, "Error", "Please set up your trade link in your profile before purchasing items.", 0);

            var listing = await _dbContext.MarketplaceListings
                .Include(l => l.Seller)
                .Include(l => l.Item)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null || listing.ListingStatus != ListingStatus.Active)
                return (false, "Error", "This item is no longer available.", 0);

            if (user.Balance < listing.Price)
                return (false, "Error", "Insufficient balance. Please add funds to your wallet.", 0);

            var trade = new Trade
            {
                ListingId = listing.Id,
                BuyerId = user.Id,
                SellerId = listing.SellerId,
                ItemId = listing.ItemId.ToString(),
                ItemName = listing.Item.Name,
                ItemWear = listing.FloatValue?.ToString("0.######"),
                Amount = listing.Price,
                Status = TradeStatus.WaitingForSeller,
                StatusMessage = "Waiting for seller to send trade offer...",
                CreatedAt = DateTime.UtcNow,
                TradeOfferUrl = user.TradeLink
            };
            _dbContext.Trades.Add(trade);
            user.Balance -= listing.Price;
            listing.ListingStatus = ListingStatus.Sold;

            var transaction = new WalletTransaction
            {
                UserId = user.Id,
                Amount = -listing.Price,
                Type = WalletTransactionType.Withdrawal,
                Description = $"Purchase: {listing.Item.Name}",
                CreatedAt = DateTime.UtcNow,
                Status = WalletTransactionStatus.Completed,
                ReferenceId = trade.Id.ToString(),
            };

            _dbContext.WalletTransactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            return (true, "Success", "Purchase successful! Please wait for the seller to send you a trade offer.", trade.Id);
        }
    }
}
