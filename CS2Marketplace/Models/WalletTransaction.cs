using System;

namespace CS2Marketplace.Models
{
    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        Payment,
        Refund
    }

    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled
    }

    public class WalletTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public TransactionType TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string PaymentId { get; set; } // Optional: Stripe payment ID or test payment identifier
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        // Navigation property
        public User User { get; set; }
    }
}
