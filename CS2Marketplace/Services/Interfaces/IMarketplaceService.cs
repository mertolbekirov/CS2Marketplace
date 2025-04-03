using System.Threading.Tasks;
using System.Collections.Generic;
using CS2Marketplace.Models;
using Microsoft.AspNetCore.Http;
using CS2Marketplace.Models.ViewModels;

namespace CS2Marketplace.Services.Interfaces
{
    public interface IMarketplaceService
    {
        Task<InventoryViewModel> LoadInventory(string currentUserSteamId);
        Task<(bool success, string tempDataKey, string tempDataMessage)> CreateListing(string steamId, string assetId, decimal price);
        Task<List<MarketplaceListing>> GetActiveListings();
        Task<(bool success, string tempDataKey, string tempDataMessage, int tradeId)> Purchase(string steamId, int listingId);
    }
} 