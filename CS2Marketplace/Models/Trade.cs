using System;

namespace CS2Marketplace.Models
{
    public enum TradeStatus
    {
        Pending,
        WaitingForPayment,
        WaitingForSeller,
        WaitingForBuyer,
        Completed,
        Failed,
        Cancelled,
        Refunded
    }

    public class Trade
    {
        public int Id { get; set; }

        public int SellerId { get; set; }
        public User Seller { get; set; }

        public int BuyerId { get; set; }
        public User Buyer { get; set; }

        public int ListingId { get; set; }
        public MarketplaceListing Listing { get; set; }

        public string ItemId { get; set; }          // Steam item asset ID
        public string ItemName { get; set; }        // Item name for display
        public string ItemWear { get; set; }        // Float value
        public decimal Amount { get; set; }
        
        public string? TradeOfferId { get; set; }    // Steam trade offer ID
        public TradeStatus Status { get; set; }
        
        public string StatusMessage { get; set; }    // Optional message about current status
        
        // Transaction tracking
        public string? PaymentIntentId { get; set; }  // Stripe payment intent ID
        public bool IsPaid { get; set; }         // Whether the payment has been processed
        public bool IsRefunded { get; set; }         // Whether the payment has been refunded
        public string? RefundId { get; set; }
        public DateTime? RefundedAt { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastChecked { get; set; }   // Last time we checked trade status
        public DateTime? TradeOfferExpiresAt { get; set; } // When the trade offer expires
        
        public bool ShouldTimeout => 
            Status == TradeStatus.WaitingForBuyer && 
            TradeOfferExpiresAt.HasValue && 
            DateTime.UtcNow > TradeOfferExpiresAt.Value;
    }
}
