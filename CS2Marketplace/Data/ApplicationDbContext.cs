using CS2Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace CS2Marketplace.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for our models
        public DbSet<User> Users { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<MarketplaceListing> MarketplaceListings { get; set; }
        public DbSet<Trade> Trades { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User relationships
            modelBuilder.Entity<User>()
                .HasMany(u => u.Listings)
                .WithOne(l => l.Seller)
                .HasForeignKey(l => l.SellerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.TradesAsBuyer)
                .WithOne(t => t.Buyer)
                .HasForeignKey(t => t.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.TradesAsSeller)
                .WithOne(t => t.Seller)
                .HasForeignKey(t => t.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.WalletTransactions)
                .WithOne(wt => wt.User)
                .HasForeignKey(wt => wt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // MarketplaceListing relationships
            modelBuilder.Entity<MarketplaceListing>()
                .HasOne(ml => ml.Item)
                .WithMany()
                .HasForeignKey(ml => ml.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Trade relationships
            modelBuilder.Entity<Trade>()
                .HasOne(t => t.Listing)
                .WithMany()
                .HasForeignKey(t => t.ListingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
