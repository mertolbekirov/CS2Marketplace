using System;

namespace CS2Marketplace.Models
{
    public enum ListingStatus
    {
        Active,
        Sold,
        Cancelled
    }

    public class MarketplaceListing
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public int ItemId { get; set; }

        // Rename to clarify that this holds the unique assetId.
        public string UniqueAssetId { get; set; }

        public decimal Price { get; set; }
        public ListingStatus ListingStatus { get; set; }
        public DateTime ListedAt { get; set; }

        // Unique item properties
        public float? FloatValue { get; set; }
        public int? PatternIndex { get; set; }

        // Navigation properties
        public User Seller { get; set; }
        public Item Item { get; set; }
    }
}
