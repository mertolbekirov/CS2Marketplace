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

        public async Task<bool> CreateTradeOffer(Trade trade)
        {
            var seller = await _dbContext.Users.FindAsync(trade.SellerId);
            if (seller == null || string.IsNullOrEmpty(seller.SteamApiKey))
                return false;

            try
            {
                // Create trade offer using Steam Web API
                var tradeOfferRequest = new
                {
                    newversion = true,
                    version = 2,
                    trade_offer_access_token = "", // Optional trade token if needed
                    steamid_other = trade.BuyerId,
                    message = $"CS2Marketplace - {trade.ItemName}",
                    items_to_give = new[]
                    {
                        new
                        {
                            appid = 730, // CS2 AppID
                            contextid = "2", // Inventory context
                            assetid = trade.ItemId
                        }
                    }
                };

                var content = JsonContent.Create(tradeOfferRequest);
                var response = await _httpClient.PostAsync(
                    $"{_steamApiBaseUrl}/IEconService/CreateTradeOffer/v1/?key={seller.SteamApiKey}",
                    content
                );

                if (!response.IsSuccessStatusCode)
                {
                    trade.StatusMessage = "Failed to create trade offer: " + await response.Content.ReadAsStringAsync();
                    trade.Status = TradeStatus.Failed;
                    await _dbContext.SaveChangesAsync();
                    await ProcessFailedTrade(trade);
                    return false;
                }

                var result = await JsonSerializer.DeserializeAsync<JsonElement>(
                    await response.Content.ReadAsStreamAsync());

                if (result.TryGetProperty("response", out var responseObj) &&
                    responseObj.TryGetProperty("tradeofferid", out var tradeOfferId))
                {
                    trade.TradeOfferId = tradeOfferId.GetString();
                    trade.Status = TradeStatus.WaitingForBuyer;
                    trade.LastChecked = DateTime.UtcNow;
                    trade.TradeOfferExpiresAt = DateTime.UtcNow.Add(_tradeOfferTimeout);
                    await _dbContext.SaveChangesAsync();
                    return true;
                }

                trade.StatusMessage = "Invalid response from Steam API";
                trade.Status = TradeStatus.Failed;
                await _dbContext.SaveChangesAsync();
                await ProcessFailedTrade(trade);
                return false;
            }
            catch (Exception ex)
            {
                trade.StatusMessage = $"Failed to create trade offer: {ex.Message}";
                trade.Status = TradeStatus.Failed;
                await _dbContext.SaveChangesAsync();
                await ProcessFailedTrade(trade);
                return false;
            }
        }

        public async Task CheckPendingTrades()
        {
            var pendingTrades = await _dbContext.Trades
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .Where(t => t.Status == TradeStatus.WaitingForBuyer)
                .Where(t => t.LastChecked == null || DateTime.UtcNow.Subtract(t.LastChecked.Value).TotalMinutes >= 5)
                .ToListAsync();

            foreach (var trade in pendingTrades)
            {
                // Check for timeout first
                if (trade.ShouldTimeout)
                {
                    trade.Status = TradeStatus.Failed;
                    trade.StatusMessage = "Trade offer expired after 6 hours";
                    await _dbContext.SaveChangesAsync();
                    await ProcessFailedTrade(trade);
                    continue;
                }

                await CheckTradeStatus(trade);
            }
        }

        private async Task CheckTradeStatus(Trade trade)
        {
            if (string.IsNullOrEmpty(trade.TradeOfferId) || trade.Seller?.SteamApiKey == null)
                return;

            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_steamApiBaseUrl}/IEconService/GetTradeOffer/v1/?key={trade.Seller.SteamApiKey}&tradeofferid={trade.TradeOfferId}&language=en");

                if (!response.IsSuccessStatusCode)
                {
                    trade.StatusMessage = "Failed to check trade status: " + await response.Content.ReadAsStringAsync();
                    await _dbContext.SaveChangesAsync();
                    return;
                }

                var result = await JsonSerializer.DeserializeAsync<JsonElement>(
                    await response.Content.ReadAsStreamAsync());

                if (result.TryGetProperty("response", out var responseObj) &&
                    responseObj.TryGetProperty("offer", out var offerObj) &&
                    offerObj.TryGetProperty("trade_offer_state", out var stateObj))
                {
                    var state = stateObj.GetInt32();
                    trade.LastChecked = DateTime.UtcNow;

                    // Steam trade offer states:
                    // 2 = Active
                    // 3 = Accepted
                    // 4 = Countered
                    // 5 = Expired
                    // 6 = Canceled
                    // 7 = Declined
                    // 8 = InvalidItems
                    // 9 = TradeBanned
                    // 10 = Pending
                    switch (state)
                    {
                        case 3: // Accepted
                            trade.Status = TradeStatus.Completed;
                            trade.CompletedAt = DateTime.UtcNow;
                            break;
                        case 4: // Countered
                        case 5: // Expired
                        case 6: // Canceled
                        case 7: // Declined
                        case 8: // InvalidItems
                        case 9: // TradeBanned
                            trade.Status = TradeStatus.Failed;
                            trade.StatusMessage = $"Trade offer {GetTradeStateMessage(state)}";
                            await ProcessFailedTrade(trade);
                            break;
                    }
                }
                
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                trade.StatusMessage = $"Failed to check trade status: {ex.Message}";
                await _dbContext.SaveChangesAsync();
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
    }
} 