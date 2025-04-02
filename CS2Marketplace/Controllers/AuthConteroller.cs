using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Models;
using CS2Marketplace.Services;
using CS2Marketplace.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CS2Marketplace.Controllers
{
    public class AuthController : Controller
    {
        private readonly SteamAuthService _steamAuthService;
        private readonly SteamApiService _steamApiService;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        public AuthController(
            SteamAuthService steamAuthService, 
            SteamApiService steamApiService, 
            IUserService userService,
            IConfiguration configuration)
        {
            _steamAuthService = steamAuthService;
            _steamApiService = steamApiService;
            _userService = userService;
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

            var username = profile?.PersonaName ?? "Unknown";
            var avatarUrl = profile?.AvatarUrl ?? "";

            // Get or create user using UserService
            var user = await _userService.GetOrCreateUserAsync(steamId, username, avatarUrl);

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
