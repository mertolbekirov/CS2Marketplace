using System;

namespace CS2Marketplace.Models
{
    public enum WalletTransactionType
    {
        Deposit,
        Withdrawal,
        Sale,
        Refund,
        Purchase
    }

    public enum WalletTransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public class WalletTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public decimal Amount { get; set; }
        public WalletTransactionType Type { get; set; }
        public WalletTransactionStatus Status { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ReferenceId { get; set; }  // Reference to related entity (e.g. trade ID)
    }
}
