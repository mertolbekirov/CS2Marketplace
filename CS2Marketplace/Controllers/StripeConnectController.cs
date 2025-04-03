using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Services;
using System.Threading.Tasks;
using CS2Marketplace.Data;
using CS2Marketplace.Services.Interfaces;
using CS2Marketplace.Filters;
using Microsoft.AspNetCore.Http;

namespace CS2Marketplace.Controllers
{
    [RequireAuthentication]
    public class StripeConnectController : Controller
    {
        private readonly IStripeConnectService _stripeConnectService;
        private readonly IUserService _userService;

        public StripeConnectController(IStripeConnectService stripeConnectService, IUserService userService)
        {
            _stripeConnectService = stripeConnectService;
            _userService = userService;
        }

        // GET: /StripeConnect/Onboard
        public async Task<IActionResult> Onboard()
        {
            string currentUserSteamId = HttpContext.Session.GetString("SteamId")!;
            var user = await _userService.GetUserBySteamIdAsync(currentUserSteamId);

            var result = await _stripeConnectService.HandleOnboard(user);
            if (!result.success)
            {
                TempData["Error"] = result.errorMessage;
                return RedirectToAction(result.redirectUrl, "Account");
            }

            return Redirect(result.redirectUrl);
        }

        // GET: /StripeConnect/OnboardingComplete
        public async Task<IActionResult> OnboardingComplete()
        {
            string currentUserSteamId = HttpContext.Session.GetString("SteamId")!;
            var user = await _userService.GetUserBySteamIdAsync(currentUserSteamId);
            if (user == null || string.IsNullOrEmpty(user.StripeConnectAccountId))
            {
                return RedirectToAction("Onboard");
            }

            var result = await _stripeConnectService.HandleOnboardingComplete(user.StripeConnectAccountId);
            if (result.success)
            {
                TempData["Message"] = result.message;
                return RedirectToAction("Profile", "Account");
            }
            else
            {
                TempData["Error"] = result.message;
                return RedirectToAction("Onboard");
            }
        }

        // GET: /StripeConnect/OnboardingRefresh
        public async Task<IActionResult> OnboardingRefresh()
        {
            string currentUserSteamId = HttpContext.Session.GetString("SteamId")!;
            var user = await _userService.GetUserBySteamIdAsync(currentUserSteamId);
            if (user == null || string.IsNullOrEmpty(user.StripeConnectAccountId))
            {
                return RedirectToAction("Onboard");
            }

            string onboardingLink = await _stripeConnectService.CreateOnboardingRefreshLink(user);
            return Redirect(onboardingLink);
        }
    }
} 