using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CS2Marketplace.Data;
using CS2Marketplace.Models;
using Stripe;
using Stripe.Checkout;

namespace CS2Marketplace.Services
{
    public class PaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;

        public PaymentService(IConfiguration configuration, ApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        /// <summary>
        /// Retrieves the existing Stripe customer ID from the user record or creates a new Stripe customer.
        /// </summary>
        public async Task<string> GetOrCreateStripeCustomerAsync(User user)
        {
            if (!string.IsNullOrEmpty(user.StripeCustomerId))
            {
                return user.StripeCustomerId;
            }

            var options = new CustomerCreateOptions
            {
                Email = user.Email,
                Name = user.Username,
                // You can add metadata or other details here.
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", user.Id.ToString() },
                    { "SteamId", user.SteamId }
                }
            };

            var service = new CustomerService();
            Customer customer = await service.CreateAsync(options);

            // Update the user record with the new Stripe customer ID.
            user.StripeCustomerId = customer.Id;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            return customer.Id;
        }

        /// <summary>
        /// Creates a Stripe Checkout Session for a deposit.
        /// </summary>
        public async Task<Session> CreateDepositSessionAsync(User user, decimal amount, string currency = "usd")
        {
            // Ensure the user has a Stripe customer record.
            string customerId = await GetOrCreateStripeCustomerAsync(user);

            // Stripe expects the amount in the smallest currency unit (e.g. cents).
            var amountInCents = (long)(amount * 100);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Customer = customerId,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = amountInCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Deposit to CS2Marketplace Account",
                                Description = $"Deposit of {amount:C} (test mode)"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = _configuration["Stripe:SuccessUrl"],
                CancelUrl = _configuration["Stripe:CancelUrl"]
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);
            return session;
        }

        /// <summary>
        /// Confirms a deposit session by retrieving it from Stripe.
        /// If the payment succeeded, returns the deposited amount (in dollars); otherwise returns null.
        /// </summary>
        public async Task<decimal?> ConfirmDepositSessionAsync(string sessionId)
        {
            var service = new SessionService();
            Session session = await service.GetAsync(sessionId);
            if (session.PaymentStatus == "paid")
            {
                // Amount_total is in cents.
                return session.AmountTotal / 100m;
            }
            return null;
        }

        /// <summary>
        /// Processes a withdrawal request by using the Stripe Payout API.
        /// For a live deployment, this would require that you set up Stripe Connect.
        /// </summary>
        public async Task<bool> ProcessWithdrawalAsync(User user, decimal amount, string currency = "usd")
        {
            // Ensure the user has a Stripe customer record.
            string customerId = await GetOrCreateStripeCustomerAsync(user);

            // In a real scenario, you might be using Stripe Connect with a connected account.
            // For simplicity here, we simulate a withdrawal by calling Stripe's Payout API on a test account.
            var amountInCents = (long)(amount * 100);
            var options = new PayoutCreateOptions
            {
                Amount = amountInCents,
                Currency = currency,
                StatementDescriptor = "CS2Marketplace Withdrawal"
            };

            var service = new PayoutService();
            try
            {
                // If you are not using Connect, you might not pass a StripeAccount parameter.
                // For live mode with Connect, pass the connected account ID.
                Payout payout = await service.CreateAsync(options);
                return payout.Status == "paid";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Withdrawal error: " + ex.Message);
                return false;
            }
        }
    }
}
