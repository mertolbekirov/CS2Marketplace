﻿// <auto-generated />
using System;
using CS2Marketplace.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CS2Marketplace.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250401104944_UpdateWalletTransactionUserIdToInt")]
    partial class UpdateWalletTransactionUserIdToInt
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CS2Marketplace.Models.Item", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ExternalItemId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Rarity")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Items");
                });

            modelBuilder.Entity("CS2Marketplace.Models.MarketplaceListing", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<float?>("FloatValue")
                        .HasColumnType("real");

                    b.Property<int>("ItemId")
                        .HasColumnType("int");

                    b.Property<DateTime>("ListedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ListingStatus")
                        .HasColumnType("int");

                    b.Property<int?>("PatternIndex")
                        .HasColumnType("int");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("SellerId")
                        .HasColumnType("int");

                    b.Property<string>("UniqueAssetId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ItemId");

                    b.HasIndex("SellerId");

                    b.ToTable("MarketplaceListings");
                });

            modelBuilder.Entity("CS2Marketplace.Models.Trade", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("BuyerId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsPaid")
                        .HasColumnType("bit");

                    b.Property<bool>("IsRefunded")
                        .HasColumnType("bit");

                    b.Property<string>("ItemId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ItemName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ItemWear")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("LastChecked")
                        .HasColumnType("datetime2");

                    b.Property<int>("ListingId")
                        .HasColumnType("int");

                    b.Property<string>("PaymentIntentId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RefundId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("RefundedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("SellerId")
                        .HasColumnType("int");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<string>("StatusMessage")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("TradeOfferExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("TradeOfferId")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("BuyerId");

                    b.HasIndex("ListingId");

                    b.HasIndex("SellerId");

                    b.ToTable("Trades");
                });

            modelBuilder.Entity("CS2Marketplace.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("APIKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AvatarUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("Balance")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("LastLogin")
                        .HasColumnType("datetime2");

                    b.Property<string>("SteamApiKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SteamId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StripeConnectAccountId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StripeConnectDashboardLink")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("StripeConnectEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("StripeConnectOnboardingLink")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StripeCustomerId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("CS2Marketplace.Models.WalletTransaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ReferenceId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("WalletTransactions");
                });

            modelBuilder.Entity("CS2Marketplace.Models.MarketplaceListing", b =>
                {
                    b.HasOne("CS2Marketplace.Models.Item", "Item")
                        .WithMany()
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("CS2Marketplace.Models.User", "Seller")
                        .WithMany("Listings")
                        .HasForeignKey("SellerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");

                    b.Navigation("Seller");
                });

            modelBuilder.Entity("CS2Marketplace.Models.Trade", b =>
                {
                    b.HasOne("CS2Marketplace.Models.User", "Buyer")
                        .WithMany("TradesAsBuyer")
                        .HasForeignKey("BuyerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("CS2Marketplace.Models.MarketplaceListing", "Listing")
                        .WithMany()
                        .HasForeignKey("ListingId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("CS2Marketplace.Models.User", "Seller")
                        .WithMany("TradesAsSeller")
                        .HasForeignKey("SellerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Buyer");

                    b.Navigation("Listing");

                    b.Navigation("Seller");
                });

            modelBuilder.Entity("CS2Marketplace.Models.WalletTransaction", b =>
                {
                    b.HasOne("CS2Marketplace.Models.User", "User")
                        .WithMany("WalletTransactions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("CS2Marketplace.Models.User", b =>
                {
                    b.Navigation("Listings");

                    b.Navigation("TradesAsBuyer");

                    b.Navigation("TradesAsSeller");

                    b.Navigation("WalletTransactions");
                });
#pragma warning restore 612, 618
        }
    }
}
