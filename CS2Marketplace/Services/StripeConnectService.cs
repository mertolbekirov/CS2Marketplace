using System;
using System.Threading.Tasks;
using CS2Marketplace.Data;
using CS2Marketplace.Models;
using CS2Marketplace.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CS2Marketplace.Services
{
    public class StripeConnectService : IStripeConnectService
    {
        private readonly IPaymentService _paymentService;
        private readonly ApplicationDbContext _dbContext;

        public StripeConnectService(IPaymentService paymentService, ApplicationDbContext dbContext)
        {
            _paymentService = paymentService;
            _dbContext = dbContext;
        }

        public async Task<(bool success, string redirectUrl, string errorMessage)> HandleOnboard(User user)
        {
            // Validate email address
            if (string.IsNullOrEmpty(user.Email) || !user.Email.Contains("@"))
            {
                return (false, "Profile", "A valid email address is required to set up Stripe Connect. Please update your profile with a valid email address.");
            }

            // If user already has a Stripe Connect account, redirect to their dashboard
            if (!string.IsNullOrEmpty(user.StripeConnectAccountId))
            {
                if (user.StripeConnectEnabled)
                {
                    return (true, user.StripeConnectDashboardLink, null);
                }
                else if (!string.IsNullOrEmpty(user.StripeConnectOnboardingLink))
                {
                    return (true, user.StripeConnectOnboardingLink, null);
                }
            }

            try
            {
                // Create a new Stripe Connect account and get the onboarding link
                string onboardingLink = await _paymentService.CreateStripeConnectAccountAsync(user);
                return (true, onboardingLink, null);
            }
            catch (Exception)
            {
                return (false, "Profile", "There was an error setting up your Stripe Connect account. Please try again later.");
            }
        }

        public async Task<(bool success, string message)> HandleOnboardingComplete(string stripeConnectAccountId)
        {
            // Update the account status
            await _paymentService.UpdateStripeConnectAccountStatusAsync(stripeConnectAccountId);

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.StripeConnectAccountId == stripeConnectAccountId);
            if (user?.StripeConnectEnabled == true)
            {
                return (true, "Your Stripe Connect account has been successfully set up!");
            }
            else
            {
                return (false, "There was an issue setting up your Stripe Connect account. Please try again.");
            }
        }

        public async Task<string> CreateOnboardingRefreshLink(User user)
        {
            return await _paymentService.CreateStripeConnectAccountAsync(user);
        }
    }
} 