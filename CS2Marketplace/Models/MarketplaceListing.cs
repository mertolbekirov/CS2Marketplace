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

        // If needed, store the unique instance id from the Steam inventory
        public string ExternalInstanceId { get; set; }

        public decimal Price { get; set; }
        public ListingStatus ListingStatus { get; set; }
        public DateTime ListedAt { get; set; }

        // Navigation properties
        public User Seller { get; set; }
        public Item Item { get; set; }
    }
}
