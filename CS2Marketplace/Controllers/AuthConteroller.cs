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
        private readonly SteamApiService _steamApiService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthController(SteamAuthService steamAuthService, SteamApiService steamApiService, ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _steamAuthService = steamAuthService;
            _steamApiService = steamApiService;
            _dbContext = dbContext;
            _configuration = configuration;
        }

        // Redirects to Steam for authentication.
        public IActionResult SignIn()
        {
            var callbackUrl = Url.Action("SteamCallback", "Auth", null, Request.Scheme);
            var loginUrl = _steamAuthService.GetSteamLoginUrl(callbackUrl);
            return Redirect(loginUrl);
        }

        // Handles the Steam callback.
        public async Task<IActionResult> SteamCallback()
        {
            var steamId = await _steamAuthService.ValidateSteamCallback(Request.Query);
            if (string.IsNullOrEmpty(steamId))
            {
                return RedirectToAction("LoginFailed");
            }

            // Get your Steam API key from configuration.
            var steamApiKey = _configuration.GetValue<string>("Steam:ApiKey");

            // Fetch the user profile data from Steam.
            var profile = await _steamApiService.GetUserProfileAsync(steamId, steamApiKey);

            // Look up or create the user.
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
            {
                user = new User
                {
                    SteamId = steamId,
                    Username = profile != null ? profile.PersonaName : "Unknown",
                    AvatarUrl = profile != null ? profile.AvatarUrl : "",
                    Email = "",
                    Balance = 0.0m,
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow
                };
                _dbContext.Users.Add(user);
            }
            else
            {
                if (profile != null)
                {
                    user.Username = profile.PersonaName;
                    user.AvatarUrl = profile.AvatarUrl;
                }
                user.LastLogin = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync();

            // Store the SteamId in session.
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
