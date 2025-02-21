using System;
using System.Collections.Generic;

namespace CS2Marketplace.Models
{
    public class User
    {
        public int Id { get; set; }
        public string SteamId { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public string Email { get; set; }
        public string APIKey { get; set; }
        public decimal Balance { get; set; } = 0.0m;
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }

        // Navigation properties
        public ICollection<MarketplaceListing> Listings { get; set; }
        public ICollection<Trade> TradesAsBuyer { get; set; }
        public ICollection<Trade> TradesAsSeller { get; set; }
        public ICollection<WalletTransaction> WalletTransactions { get; set; }
    }
}
