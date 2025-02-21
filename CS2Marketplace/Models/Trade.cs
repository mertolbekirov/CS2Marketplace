using System;

namespace CS2Marketplace.Models
{
    public enum TradeStatus
    {
        Pending,
        Completed,
        Cancelled
    }

    public class Trade
    {
        public int Id { get; set; }
        public int BuyerId { get; set; }
        public int SellerId { get; set; }
        public int ListingId { get; set; }
        public decimal Amount { get; set; }  // Cash paid by the buyer
        public string PaymentId { get; set; } // Payment transaction ID from Stripe or test gateway
        public TradeStatus TradeStatus { get; set; }
        public DateTime TradeInitiatedAt { get; set; }
        public DateTime? TradeCompletedAt { get; set; }

        // Navigation properties
        public User Buyer { get; set; }
        public User Seller { get; set; }
        public MarketplaceListing Listing { get; set; }
    }
}
