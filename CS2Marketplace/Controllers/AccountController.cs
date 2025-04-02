using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using CS2Marketplace.Models;
using System;
using CS2Marketplace.Services.Interfaces;

namespace CS2Marketplace.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISteamVerificationService steamVerificationService;

        public AccountController(ApplicationDbContext dbContext, ISteamVerificationService steamVerificationService)
        {
            _dbContext = dbContext;
            this.steamVerificationService = steamVerificationService;
        }

        // GET: /Account/Profile
        public async Task<IActionResult> Profile()
        {
            var steamId = HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamId))
            {
                return RedirectToAction("SignIn", "Auth");
            }

            // Find the user in the database and include their wallet transactions
            var user = await _dbContext.Users
                .Include(u => u.WalletTransactions)
                .FirstOrDefaultAsync(u => u.SteamId == steamId);
                
            if (user == null)
            {
                return RedirectToAction("SignIn", "Auth");
            }

            return View(user);
        }

        // POST: /Account/UpdateEmail
        [HttpPost]
        public async Task<IActionResult> UpdateEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction("Profile");
            }

            var steamId = HttpContext.Session.GetString("SteamId");
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            
            if (user == null)
            {
                return RedirectToAction("SignIn", "Auth");
            }

            user.Email = email;
            await _dbContext.SaveChangesAsync();

            TempData["Message"] = "Your email address has been updated successfully.";
            return RedirectToAction("Profile");
        }

        // POST: /Account/UpdateSteamApiKey
        [HttpPost]
        public async Task<IActionResult> UpdateSteamApiKey(string steamApiKey)
        {
            var steamId = HttpContext.Session.GetString("SteamId");
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            
            if (user == null)
            {
                return RedirectToAction("SignIn", "Auth");
            }

            user.SteamApiKey = steamApiKey;
            await _dbContext.SaveChangesAsync();

            var isApiKeyRemoved = string.IsNullOrEmpty(steamApiKey);

            TempData["Message"] = isApiKeyRemoved
                ? "Your Steam API Key has been removed."
                : "Your Steam API Key has been updated successfully.";

            if (!isApiKeyRemoved)
            {
                var isApiKeyCorrectandIsUserEligibleForTrade = await steamVerificationService.VerifyUserEligibilityAsync(user);
                if (!isApiKeyCorrectandIsUserEligibleForTrade)
                {
                    TempData["Message"] += " However, your account is not eligible for trading. Please check your API key and Steam Account status.";
                }
            }

            return RedirectToAction("Profile");
        }

        // POST: /Account/UpdateTradeLink
        [HttpPost]
        public async Task<IActionResult> UpdateTradeLink(string tradeLink)
        {
            var steamId = HttpContext.Session.GetString("SteamId");
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);

            if (user == null)
            {
                return RedirectToAction("SignIn", "Auth");
            }

            user.TradeLink = tradeLink;
            await _dbContext.SaveChangesAsync();

            TempData["Message"] = string.IsNullOrEmpty(tradeLink)
                ? "Your Trade Link has been removed."
                : "Your Trade Link has been updated successfully.";
            return RedirectToAction("Profile");
        }
    }
}
