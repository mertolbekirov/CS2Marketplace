using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Services;
using System.Threading.Tasks;
using CS2Marketplace.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace CS2Marketplace.Controllers
{
    public class StripeConnectController : Controller
    {
        private readonly PaymentService _paymentService;
        private readonly ApplicationDbContext _dbContext;

        public StripeConnectController(PaymentService paymentService, ApplicationDbContext dbContext)
        {
            _paymentService = paymentService;
            _dbContext = dbContext;
        }

        // GET: /StripeConnect/Onboard
        public async Task<IActionResult> Onboard()
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
            {
                return RedirectToAction("SignIn", "Auth");
            }

            // Validate email address
            if (string.IsNullOrEmpty(user.Email) || !user.Email.Contains("@"))
            {
                TempData["Error"] = "A valid email address is required to set up Stripe Connect. Please update your profile with a valid email address.";
                return RedirectToAction("Profile", "Account");
            }

            // If user already has a Stripe Connect account, redirect to their dashboard
            if (!string.IsNullOrEmpty(user.StripeConnectAccountId))
            {
                if (user.StripeConnectEnabled)
                {
                    return Redirect(user.StripeConnectDashboardLink);
                }
                else if (!string.IsNullOrEmpty(user.StripeConnectOnboardingLink))
                {
                    return Redirect(user.StripeConnectOnboardingLink);
                }
            }

            try
            {
                // Create a new Stripe Connect account and get the onboarding link
                string onboardingLink = await _paymentService.CreateStripeConnectAccountAsync(user);
                return Redirect(onboardingLink);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "There was an error setting up your Stripe Connect account. Please try again later.";
                return RedirectToAction("Profile", "Account");
            }
        }

        // GET: /StripeConnect/OnboardingComplete
        public async Task<IActionResult> OnboardingComplete()
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null || string.IsNullOrEmpty(user.StripeConnectAccountId))
            {
                return RedirectToAction("Onboard");
            }

            // Update the account status
            await _paymentService.UpdateStripeConnectAccountStatusAsync(user.StripeConnectAccountId);

            if (user.StripeConnectEnabled)
            {
                TempData["Message"] = "Your Stripe Connect account has been successfully set up!";
                return RedirectToAction("Profile", "Account");
            }
            else
            {
                TempData["Error"] = "There was an issue setting up your Stripe Connect account. Please try again.";
                return RedirectToAction("Onboard");
            }
        }

        // GET: /StripeConnect/OnboardingRefresh
        public async Task<IActionResult> OnboardingRefresh()
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null || string.IsNullOrEmpty(user.StripeConnectAccountId))
            {
                return RedirectToAction("Onboard");
            }

            // Create a new onboarding link
            string onboardingLink = await _paymentService.CreateStripeConnectAccountAsync(user);
            return Redirect(onboardingLink);
        }
    }
} 