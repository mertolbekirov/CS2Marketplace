using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CS2Marketplace.Data;
using CS2Marketplace.Models;
using CS2Marketplace.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CS2Marketplace.Services
{
    public class SteamVerificationService : ISteamVerificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _dbContext;


        public SteamVerificationService(HttpClient httpClient, IConfiguration configuration, ApplicationDbContext dbContext)
        {
            _httpClient = httpClient;
            this._dbContext = dbContext;
        }

        public async Task<bool> VerifyUserEligibilityAsync(User user)
        {
            if (string.IsNullOrEmpty(user.SteamApiKey))
            {
                user.VerificationMessage = "No API key configured";
                return false;
            }

            // Check if verification was done recently (within last 24 hours)
            if (user.LastVerificationCheck.HasValue && 
                (DateTime.UtcNow - user.LastVerificationCheck.Value).TotalHours < 24)
            {
                return user.IsEligibleForTrading;
            }

            try
            {
                // Check if user is trade banned
                var response = await _httpClient.GetAsync(
                    $"https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key={user.SteamApiKey}&steamids={user.SteamId}");

                if (!response.IsSuccessStatusCode)
                {
                    user.IsEligibleForTrading = false;
                    user.VerificationMessage = "Could not get GetPlayerBans response";
                    user.LastVerificationCheck = DateTime.UtcNow;
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SteamPlayerBansResponse>(content);

                if (result?.players == null || result.players.Length == 0)
                {
                    user.IsEligibleForTrading = false;
                    user.VerificationMessage = "Could not find user information";
                    user.LastVerificationCheck = DateTime.UtcNow;
                    return false;
                }

                var player = result.players[0];
                var isEligible = player.EconomyBan == "none" && !player.VACBanned && !player.CommunityBanned;

                user.IsEligibleForTrading = isEligible;
                user.VerificationMessage = isEligible ? 
                    "User is eligible for trading" : 
                    $"User has restrictions: {(player.EconomyBan != "none" ? "Economy Ban, " : "")}{(player.VACBanned ? "VAC Ban, " : "")}{(player.CommunityBanned ? "Community Ban" : "")}";
                user.LastVerificationCheck = DateTime.UtcNow;

                _dbContext.SaveChanges();

                return isEligible;
            }
            catch (Exception ex)
            {
                user.IsEligibleForTrading = false;
                user.VerificationMessage = "Error verifying eligibility";
                user.LastVerificationCheck = DateTime.UtcNow;
                return false;
            }
        }
    }

    public class SteamPlayerBansResponse
    {
        public SteamPlayerBan[] players { get; set; }
    }

    public class SteamPlayerBan
    {
        public string SteamId { get; set; }
        public bool CommunityBanned { get; set; }
        public bool VACBanned { get; set; }
        public int NumberOfVACBans { get; set; }
        public int DaysSinceLastBan { get; set; }
        public int NumberOfGameBans { get; set; }
        public string EconomyBan { get; set; }
    }
} 