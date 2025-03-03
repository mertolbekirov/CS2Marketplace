﻿using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using CS2Marketplace.Models;
using CS2Marketplace.Data;
using CS2Marketplace.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Extensions.Caching.Memory;
using CS2Marketplace.Models.ViewModels;

namespace CS2Marketplace.Controllers
{
    public class MarketplaceController : Controller
    {
        private readonly SteamApiService _steamApiService;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly IMemoryCache _cache;

        public MarketplaceController(SteamApiService steamApiService, IConfiguration configuration, ApplicationDbContext dbContext, IMemoryCache cache)
        {
            _steamApiService = steamApiService;
            _configuration = configuration;
            _dbContext = dbContext;
            _cache = cache;
        }

        // GET: /Marketplace/LoadInventory
        public async Task<IActionResult> LoadInventory()
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamId))
            {
                return RedirectToAction("SignIn", "Auth");
            }

            // Determine if force refresh is requested.
            bool forceRefresh = Request.Query["force"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);

            // Cache key based on steamId.
            List<InventoryItem> inventory;

                // Not in cache or forced refresh: load from Steam API.
                string appId = _configuration.GetValue<string>("Steam:AppId") ?? "730";
                string contextId = _configuration.GetValue<string>("Steam:ContextId") ?? "2";
                inventory = await _steamApiService.GetPlayerInventoryAsync(steamId, appId, contextId);


            // Retrieve active listing assetIds for the current user.
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            var activeListingIds = new List<string>();
            if (user != null)
            {
                activeListingIds = await _dbContext.MarketplaceListings
                    .Where(l => l.SellerId == user.Id && l.ListingStatus == ListingStatus.Active)
                    .Select(l => l.UniqueAssetId)
                    .ToListAsync();
            }
            ViewBag.ActiveListingIds = activeListingIds;

            return View(inventory);
        }

        // POST: /Marketplace/CreateListing
        [HttpPost]
        public async Task<IActionResult> CreateListing(string assetId, decimal price)
        {
            // Ensure the user is signed in.
            string steamId = HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamId))
            {
                return RedirectToAction("SignIn", "Auth");
            }

            // Get the current user.
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
            {
                return RedirectToAction("SignIn", "Auth");
            }

            // Prevent duplicate active listings for the same asset.
            bool alreadyListed = await _dbContext.MarketplaceListings.AnyAsync(l =>
                l.SellerId == user.Id &&
                l.UniqueAssetId == assetId &&
                l.ListingStatus == ListingStatus.Active);

            if (alreadyListed)
            {
                TempData["Error"] = "This item is already listed for sale.";
                return RedirectToAction("LoadInventory", "Marketplace");
            }

            // Retrieve the cached inventory for this user.
            string cacheKey = $"inventory_{steamId}";
            List<InventoryItem> inventory = null;
            if (!_cache.TryGetValue(cacheKey, out inventory))
            {
                // If inventory is not in cache, load it (using the same appId and contextId from configuration).
                string appId = _configuration.GetValue<string>("Steam:AppId") ?? "730";
                string contextId = _configuration.GetValue<string>("Steam:ContextId") ?? "2";
                inventory = await _steamApiService.GetPlayerInventoryAsync(steamId, appId, contextId);
            }

            // Find the inventory item that matches the assetId.
            var invItem = inventory.FirstOrDefault(i => i.AssetId == assetId);
            if (invItem == null)
            {
                TempData["Error"] = "Item not found in inventory.";
                return RedirectToAction("LoadInventory", "Marketplace");
            }

            // Check if the Item record exists in the database; if not, create it.
            var item = await _dbContext.Items.FirstOrDefaultAsync(i => i.ExternalItemId == assetId);
            if (item == null)
            {
                item = new Item
                {
                    ExternalItemId = assetId,
                    Name = invItem.Name,
                    Description = "",
                    ImageUrl = invItem.ImageUrl,
                    Rarity = ""
                };
                _dbContext.Items.Add(item);
                await _dbContext.SaveChangesAsync();
            }

            // Create a new marketplace listing using data from the inventory item.
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

            return RedirectToAction("Index", "Marketplace");
        }

        // GET: /Marketplace/Index - shows active listings.
        public async Task<IActionResult> Index()
        {
            var listings = await _dbContext.MarketplaceListings
                .Include(l => l.Item)
                .Include(l => l.Seller)
                .Where(l => l.ListingStatus == ListingStatus.Active)
                .ToListAsync();
            return View(listings);
        }
    }
}
