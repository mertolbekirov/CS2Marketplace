using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using CS2Marketplace.Data;
using CS2Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using Stripe;
using System.Text;
using System.Linq;

namespace CS2Marketplace.Services
{
    public class SteamTradeService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly string _steamApiBaseUrl = "https://api.steampowered.com";
        private readonly TimeSpan _tradeOfferTimeout = TimeSpan.FromHours(6);

        public SteamTradeService(
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _httpClient = httpClient;
        }

        public string GetSteamApiKeyUrl()
        {
            return "https://steamcommunity.com/dev/apikey";
        }

        public async Task<bool> ValidateApiKey(string apiKey)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_steamApiBaseUrl}/ISteamUser/GetPlayerSummaries/v2/?key={apiKey}&steamids=76561197960435530");
                
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private string ExtractTokenFromTradeLink(string tradeLink)
        {
            try
            {
                var uri = new Uri(tradeLink);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                return query["token"];
            }
            catch
            {
                return null;
            }
        }

        private async Task ProcessFailedTrade(Trade trade)
        {
            if (trade.IsRefunded)
                return;

            try
            {
                // Cancel the Steam trade offer if it exists
                if (!string.IsNullOrEmpty(trade.TradeOfferId) && trade.Seller?.SteamApiKey != null)
                {
                    await CancelSteamTradeOffer(trade.TradeOfferId, trade.Seller.SteamApiKey);
                }

                // Create wallet transaction for the refund
                var walletTransaction = new WalletTransaction
                {
                    UserId = trade.BuyerId,
                    Amount = trade.Amount,
                    Type = WalletTransactionType.Refund,
                    Description = $"Refund for failed trade: {trade.ItemName}",
                    CreatedAt = DateTime.UtcNow,
                    Status = WalletTransactionStatus.Completed,
                    ReferenceId = trade.Id.ToString()
                };

                _dbContext.WalletTransactions.Add(walletTransaction);

                // Update buyer's balance
                var buyer = await _dbContext.Users.FindAsync(trade.BuyerId);
                if (buyer != null)
                {
                    buyer.Balance += trade.Amount;
                }

                // Update trade record
                trade.IsRefunded = true;
                trade.RefundedAt = DateTime.UtcNow;
                trade.Status = TradeStatus.Refunded;

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                trade.StatusMessage = $"Failed to process refund: {ex.Message}";
                await _dbContext.SaveChangesAsync();
                // Log this error for manual review
                // TODO: Add proper error logging
            }
        }

        private async Task CancelSteamTradeOffer(string tradeOfferId, string steamApiKey)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("key", steamApiKey),
                    new KeyValuePair<string, string>("tradeofferid", tradeOfferId)
                });

                var response = await _httpClient.PostAsync(
                    $"{_steamApiBaseUrl}/IEconService/CancelTradeOffer/v1/",
                    content
                );

                if (!response.IsSuccessStatusCode)
                {
                    // Log the error but continue with the refund process
                    Console.WriteLine($"Failed to cancel Steam trade offer {tradeOfferId}: {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue with the refund process
                Console.WriteLine($"Error canceling Steam trade offer {tradeOfferId}: {ex.Message}");
            }
        }

        private string GetTradeStateMessage(int state) => state switch
        {
            4 => "was countered",
            5 => "expired",
            6 => "was canceled",
            7 => "was declined",
            8 => "contained invalid items",
            9 => "failed due to trade ban",
            _ => "failed with unknown reason"
        };

        private class SteamApiResponse
        {
            public TradeOfferResponse Response { get; set; }
        }

        private class TradeOfferResponse
        {
            public int Success { get; set; }
            public string TradeOfferId { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
} 