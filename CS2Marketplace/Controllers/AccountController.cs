using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CS2Marketplace.Models;
using CS2Marketplace.Services.Interfaces;
using CS2Marketplace.Filters;
using Microsoft.AspNetCore.Http;

namespace CS2Marketplace.Controllers
{
    [RequireAuthentication]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> Profile()
        {
            var steamId = HttpContext.Session.GetString("SteamId");
            var user = await _userService.GetUserWithTransactionsAsync(steamId);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction("Profile");
            }

            var steamId = HttpContext.Session.GetString("SteamId");
            await _userService.UpdateUserEmailAsync(steamId, email);
            TempData["Message"] = "Your email address has been updated successfully.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSteamApiKey(string steamApiKey)
        {
            var steamId = HttpContext.Session.GetString("SteamId");
            await _userService.UpdateUserSteamApiKeyAsync(steamId, steamApiKey);

            var isApiKeyRemoved = string.IsNullOrEmpty(steamApiKey);
            TempData["Message"] = isApiKeyRemoved
                ? "Your Steam API Key has been removed."
                : "Your Steam API Key has been updated successfully.";

            if (!isApiKeyRemoved)
            {
                var user = await _userService.GetUserBySteamIdAsync(steamId);
                var isEligible = await _userService.VerifyUserEligibilityAsync(user);
                if (!isEligible)
                {
                    TempData["Message"] += " However, your account is not eligible for trading. Please check your API key and Steam Account status.";
                }
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTradeLink(string tradeLink)
        {
            var steamId = HttpContext.Session.GetString("SteamId");
            await _userService.UpdateUserTradeLinkAsync(steamId, tradeLink);

            TempData["Message"] = string.IsNullOrEmpty(tradeLink)
                ? "Your Trade Link has been removed."
                : "Your Trade Link has been updated successfully.";
            return RedirectToAction("Profile");
        }
    }
}
