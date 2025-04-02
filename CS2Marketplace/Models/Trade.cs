using System;
using System.ComponentModel.DataAnnotations;

namespace CS2Marketplace.Models
{
    public enum TradeStatus
    {
        WaitingForSeller,          // Initial state, seller needs to send trade offer
        OfferSent,                // Seller has marked trade as sent
        WaitingForBuyerConfirmation, // Waiting for buyer to confirm receipt
        Completed,                // Buyer confirmed receipt
        Disputed,                 // Buyer reported issue
        DisputeResolved,          // Admin resolved the dispute
        Cancelled,                // Trade was cancelled
        Refunded,                 // Buyer was refunded
        Expired                   // Buyer didn't respond within time limit
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
        public string? ItemWear { get; set; }       // Float value
        public decimal Amount { get; set; }
        
        public string? TradeOfferId { get; set; }    // Steam trade offer ID
        public TradeStatus Status { get; set; }
        
        public string? StatusMessage { get; set; }    // Optional message about current status
        public string? DisputeReason { get; set; }   // Reason if buyer disputes
        public string? DisputeResolution { get; set; } // How the dispute was resolved
        public string? AdminNotes { get; set; }      // Notes from admin review
        
        public DateTime CreatedAt { get; set; }
        public DateTime? OfferSentAt { get; set; }   // When seller marked as sent
        public DateTime? CompletedAt { get; set; }   // When buyer confirmed or auto-completed
        public DateTime? DisputedAt { get; set; }    // When dispute was raised
        public DateTime? ResolvedAt { get; set; }    // When dispute was resolved
        public DateTime? LastChecked { get; set; }   // Last time status was updated
        public DateTime? BuyerResponseDeadline { get; set; } // 24-hour deadline for buyer to respond

        public bool IsRefunded { get; set; }
        public DateTime? RefundedAt { get; set; }
        
        public string TradeOfferUrl { get; set; }   // Buyer's trade URL

        // Helper properties
        public bool IsPastDeadline => 
            Status == TradeStatus.WaitingForBuyerConfirmation && 
            BuyerResponseDeadline.HasValue && 
            DateTime.UtcNow > BuyerResponseDeadline.Value;

        public bool CanBuyerRespond =>
            Status == TradeStatus.WaitingForBuyerConfirmation &&
            !IsPastDeadline;

        public bool RequiresAdminReview =>
            Status == TradeStatus.Disputed;
    }
}
