using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CS2Marketplace.Data;
using CS2Marketplace.Models;
using Stripe;
using Stripe.Checkout;
using CS2Marketplace.Services.Interfaces;

namespace CS2Marketplace.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserService _userService;

        public bool IsTestMode { get; init; }

        public PaymentService(IConfiguration configuration, ApplicationDbContext dbContext, IUserService userService)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
            IsTestMode = _configuration["Stripe:SecretKey"].StartsWith("sk_test_");
            _userService = userService;
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
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", user.Id.ToString() },
                    { "SteamId", user.SteamId }
                }
            };

            var service = new CustomerService();
            Customer customer = await service.CreateAsync(options);

            user.StripeCustomerId = customer.Id;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            return customer.Id;
        }

        /// <summary>
        /// Creates a Stripe Connect account for a user and returns the onboarding link.
        /// </summary>
        public async Task<string> CreateStripeConnectAccountAsync(User user)
        {
            var options = new AccountCreateOptions
            {
                Type = "express",
                Email = user.Email,
                BusinessProfile = new AccountBusinessProfileOptions
                {
                    Name = user.Username,
                    Mcc = "5815", // Digital goods
                    ProductDescription = "CS2 Marketplace Seller",
                    Url = _configuration["ApplicationUrl"] // Add your marketplace URL
                },
                Capabilities = new AccountCapabilitiesOptions
                {
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true }
                },
                Settings = new AccountSettingsOptions
                {
                    Payouts = new AccountSettingsPayoutsOptions
                    {
                        Schedule = new AccountSettingsPayoutsScheduleOptions
                        {
                            Interval = "daily"
                        },
                        DebitNegativeBalances = true
                    },
                    CardPayments = new AccountSettingsCardPaymentsOptions
                    {
                        DeclineOn = new AccountSettingsCardPaymentsDeclineOnOptions
                        {
                            AvsFailure = true,
                            CvcFailure = true
                        }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", user.Id.ToString() },
                    { "SteamId", user.SteamId },
                    { "Platform", "CS2Marketplace" }
                },
                BusinessType = "individual"
            };

            try
            {
                var service = new AccountService();
                Account account = await service.CreateAsync(options);

                // Create an account link for onboarding
                var linkOptions = new AccountLinkCreateOptions
                {
                    Account = account.Id,
                    RefreshUrl = _configuration["Stripe:ConnectOnboardingRefreshUrl"],
                    ReturnUrl = _configuration["Stripe:ConnectOnboardingReturnUrl"],
                    Type = "account_onboarding",
                    Collect = "eventually_due"
                };

                var linkService = new AccountLinkService();
                AccountLink link = await linkService.CreateAsync(linkOptions);

                // Update user with Stripe Connect account information
                user.StripeConnectAccountId = account.Id;
                user.StripeConnectOnboardingLink = link.Url;
                user.StripeConnectEnabled = false;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();

                return link.Url;

            }
            catch (Exception ex)
            {
                ;
                throw;
            }




        }

        /// <summary>
        /// Creates a Stripe Checkout Session for a deposit.
        /// </summary>
        public async Task<Session> CreateDepositSessionAsync(string steamId, decimal amount, string currency = "eur")
        {
            var user = await _userService.GetUserBySteamIdAsync(steamId);
            string customerId = await GetOrCreateStripeCustomerAsync(user);
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
                                Description = $"Deposit of {amount:C}"
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
        /// Confirms a deposit session and updates the user's balance.
        /// </summary>
        public async Task<decimal?> ConfirmDepositSessionAsync(string sessionId)
        {
            var service = new SessionService();
            Session session = await service.GetAsync(sessionId);
            
            if (session.PaymentStatus == "paid")
            {
                decimal amount = (decimal)(session.AmountTotal ?? 0) / 100m;
                
                // Get the user from the customer ID
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == session.CustomerId);
                if (user != null)
                {
                    user.Balance += amount;
                    
                    // Record the transaction
                    var transaction = new WalletTransaction
                    {
                        UserId = int.Parse(user.Id.ToString()),
                        Amount = amount,
                        Type = WalletTransactionType.Deposit,
                        Description = "Stripe payment",
                        CreatedAt = DateTime.UtcNow,
                        Status = WalletTransactionStatus.Completed,
                        ReferenceId = session.PaymentIntentId
                    };
                    
                    _dbContext.WalletTransactions.Add(transaction);
                    await _dbContext.SaveChangesAsync();
                }
                
                return amount;
            }
            return null;
        }

        /// <summary>
        /// Processes a withdrawal request using Stripe Connect.
        /// </summary>
        public async Task<(bool success, string errorMessage)> ProcessWithdrawalAsync(User user, decimal amount, string currency = "eur")
        {
            if (string.IsNullOrEmpty(user.StripeConnectAccountId))
                return (false, "User has no connected Stripe account.");

            try
            {
                var service = new TransferService();
                var options = new TransferCreateOptions
                {
                    Amount = (long)(amount * 100), // Convert to cents
                    Currency = currency,
                    Destination = user.StripeConnectAccountId,
                    Description = $"Withdrawal for {user.Username}"
                };

                Transfer transfer = await service.CreateAsync(options);

                // Check transfer status based on metadata
                if (!transfer.Reversed)
                {
                    // Create a withdrawal transaction
                    var withdrawal = new WalletTransaction
                    {
                        UserId = user.Id,
                        Amount = -amount, // Negative amount for withdrawal
                        Type = WalletTransactionType.Withdrawal,
                        Description = $"Withdrawal to Stripe account",
                        Status = WalletTransactionStatus.Completed,
                        CreatedAt = DateTime.UtcNow,
                        ReferenceId = transfer.Id
                    };

                    _dbContext.WalletTransactions.Add(withdrawal);
                    user.Balance -= amount;
                    await _dbContext.SaveChangesAsync();
                    
                    return (true, "Withdrawal processed successfully.");
                }
                else // transfer.Reversed is true
                {
                    return (false, "Withdrawal failed - transfer was reversed.");
                }
            }
            catch (StripeException e)
            {
                return (false, $"Stripe error: {e.Message}");
            }
            catch (Exception)
            {
                return (false, "System error processing withdrawal.");
            }
        }

        /// <summary>
        /// Updates the user's Stripe Connect account status.
        /// </summary>
        public async Task UpdateStripeConnectAccountStatusAsync(string accountId)
        {
            var service = new AccountService();
            Account account = await service.GetAsync(accountId);
            
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.StripeConnectAccountId == accountId);
            if (user != null)
            {
                user.StripeConnectEnabled = account.ChargesEnabled && account.PayoutsEnabled;
                // Use different dashboard URLs for test and live mode
                user.StripeConnectDashboardLink = IsTestMode
                    ? $"https://dashboard.stripe.com/{accountId}/test/dashboard"
                    : $"https://connect.stripe.com/app/express#acct_{accountId}/overview";
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Creates a test charge to simulate available balance in test mode
        /// </summary>
        public async Task<bool> CreateTestChargeForAvailableBalance(decimal amount)
        {
            if (!IsTestMode)
                return false;

            var options = new ChargeCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = "eur",
                Source = "tok_bypassPending", // Special token that bypasses pending state
                Description = "Test charge for available balance"
            };

            var service = new ChargeService();
            var charge = await service.CreateAsync(options);
            
            return charge.Status == "succeeded";
        }
    }
}
