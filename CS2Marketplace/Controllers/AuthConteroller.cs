using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Data;
using CS2Marketplace.Models;
using CS2Marketplace.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CS2Marketplace.Controllers
{
    public class AuthController : Controller
    {
        private readonly SteamAuthService _steamAuthService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthController(SteamAuthService steamAuthService, ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _steamAuthService = steamAuthService;
            _dbContext = dbContext;
            _configuration = configuration;
        }

        // Redirects user to Steam for authentication.
        public IActionResult SignIn()
        {
            var callbackUrl = Url.Action("SteamCallback", "Auth", null, Request.Scheme);
            var loginUrl = _steamAuthService.GetSteamLoginUrl(callbackUrl);
            return Redirect(loginUrl);
        }

        // Handles Steam's callback.
        public async Task<IActionResult> SteamCallback()
        {
            var steamId = await _steamAuthService.ValidateSteamCallback(Request.Query);
            if (string.IsNullOrEmpty(steamId))
            {
                return RedirectToAction("LoginFailed");
            }

            // Retrieve your Steam API key from configuration.
            var steamApiKey = _configuration.GetValue<string>("Steam:ApiKey");

            // Fetch the Steam profile (username and avatar)
            var profile = await _steamAuthService.GetUserProfileAsync(steamId, steamApiKey);

            // Look up or create a user record.
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
            {
                user = new User
                {
                    SteamId = steamId,
                    Username = profile != null ? profile.PersonaName : "Unknown",
                    AvatarUrl = profile != null ? profile.AvatarUrl : "",
                    Email = "",
                    APIKey = "",
                    Balance = 0.0m,
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow
                };
                _dbContext.Users.Add(user);
            }
            else
            {
                // Update existing user profile if necessary.
                if (profile != null)
                {
                    user.Username = profile.PersonaName;
                    user.AvatarUrl = profile.AvatarUrl;
                }
                user.LastLogin = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync();

            // Mark user as authenticated by storing SteamId in session.
            HttpContext.Session.SetString("SteamId", steamId);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult LoginFailed()
        {
            return View();
        }

        public IActionResult SignOut()
        {
            HttpContext.Session.Remove("SteamId");
            return RedirectToAction("Index", "Home");
        }
    }
}
