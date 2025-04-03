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
        internal readonly bool _isTestMode;

        public PaymentService(IConfiguration configuration, ApplicationDbContext dbContext, IUserService userService)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
            _isTestMode = _configuration["Stripe:SecretKey"].StartsWith("sk_test_");
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
            if (!user.StripeConnectEnabled || string.IsNullOrEmpty(user.StripeConnectAccountId))
            {
                return (false, "User has not completed Stripe Connect onboarding");
            }

            var amountInCents = (long)(amount * 100);
            
            try
            {
                // Create a Transfer to the connected account
                var options = new TransferCreateOptions
                {
                    Amount = amountInCents,
                    Currency = currency,
                    Destination = user.StripeConnectAccountId
                };

                var service = new TransferService();
                Transfer transfer = await service.CreateAsync(options);
                
                if (transfer == null || string.IsNullOrEmpty(transfer.Id))
                {
                    return (false, "Failed to create transfer: No transfer ID received");
                }

                // Record the transaction
                var transaction = new WalletTransaction
                {
                    UserId = int.Parse(user.Id.ToString()),
                    Amount = amount,
                    Type = WalletTransactionType.Withdrawal,
                    Description = "Stripe payout",
                    CreatedAt = DateTime.UtcNow,
                    Status = WalletTransactionStatus.Pending,
                    ReferenceId = transfer.Id
                };

                user.Balance -= amount;

                _dbContext.WalletTransactions.Add(transaction);
                await _dbContext.SaveChangesAsync();
                
                return (true, null);
            }
            catch (StripeException ex)
            {
                // Handle specific Stripe errors
                string errorMessage = ex.Message;
                if (ex.StripeError?.Type == "card_error")
                {
                    errorMessage = "Card error: " + ex.StripeError.Message;
                }
                else if (ex.StripeError?.Type == "invalid_request_error")
                {
                    errorMessage = "Invalid request: " + ex.StripeError.Message;
                }
                else if (ex.StripeError?.Type == "api_error")
                {
                    errorMessage = "Stripe API error: " + ex.StripeError.Message;
                }

                Console.WriteLine($"Stripe withdrawal error: {errorMessage}");
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected withdrawal error: {ex.Message}");
                return (false, "An unexpected error occurred while processing the withdrawal");
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
                user.StripeConnectDashboardLink = _isTestMode
                    ? $"https://dashboard.stripe.com/{accountId}/test/dashboard"
                    : $"https://connect.stripe.com/app/express#acct_{accountId}/overview";
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Handles Stripe webhook events for transfer status updates.
        /// </summary>
        public async Task HandleTransferWebhookAsync(string eventType, string transferId)
        {
            var transaction = await _dbContext.WalletTransactions
                .FirstOrDefaultAsync(t => t.ReferenceId == transferId && t.Type == WalletTransactionType.Withdrawal);

            if (transaction == null)
            {
                return;
            }

            switch (eventType)
            {
                case "transfer.paid":
                    transaction.Status = WalletTransactionStatus.Completed;
                    break;
                case "transfer.failed":
                    transaction.Status = WalletTransactionStatus.Failed;
                    // If the transfer failed, we should refund the user's balance
                    var user = await _dbContext.Users.FindAsync(transaction.UserId);
                    if (user != null)
                    {
                        user.Balance += transaction.Amount;
                    }
                    break;
                case "transfer.canceled":
                    transaction.Status = WalletTransactionStatus.Failed;
                    // If the transfer was cancelled, we should refund the user's balance
                    user = await _dbContext.Users.FindAsync(transaction.UserId);
                    if (user != null)
                    {
                        user.Balance += transaction.Amount;
                    }
                    break;
            }

            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a test charge to simulate available balance in test mode
        /// </summary>
        public async Task<bool> CreateTestChargeForAvailableBalance(decimal amount)
        {
            if (!_isTestMode)
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
