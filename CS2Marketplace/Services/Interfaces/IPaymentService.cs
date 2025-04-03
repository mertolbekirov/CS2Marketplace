using CS2Marketplace.Models;
using Stripe.Checkout;

namespace CS2Marketplace.Services.Interfaces
{
    public interface IPaymentService
    {
        bool IsTestMode { get; init; }

        Task<string> GetOrCreateStripeCustomerAsync(User user);
        Task<string> CreateStripeConnectAccountAsync(User user);

        Task<Session> CreateDepositSessionAsync(string steamId, decimal amount, string currency = "eur");

        Task<decimal?> ConfirmDepositSessionAsync(string sessionId);
        Task<(bool success, string errorMessage)> ProcessWithdrawalAsync(User user, decimal amount, string currency = "eur");
        Task UpdateStripeConnectAccountStatusAsync(string accountId);

        Task HandleTransferWebhookAsync(string eventType, string transferId);

        Task<bool> CreateTestChargeForAvailableBalance(decimal amount);
    }
}
