using System.Threading.Tasks;
using CS2Marketplace.Data;
using CS2Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using CS2Marketplace.Services.Interfaces;

namespace CS2Marketplace.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly SteamApiService _steamApiService;

        public UserService(ApplicationDbContext dbContext, SteamApiService steamApiService)
        {
            _dbContext = dbContext;
            _steamApiService = steamApiService;
        }

        public async Task<User> GetUserBySteamIdAsync(string steamId)
        {
            return await _dbContext.Users
                .FirstOrDefaultAsync(u => u.SteamId == steamId);
        }

        public async Task<User> GetUserWithTransactionsAsync(string steamId)
        {
            return await _dbContext.Users
                .Include(u => u.WalletTransactions)
                .FirstOrDefaultAsync(u => u.SteamId == steamId);
        }

        public async Task UpdateUserEmailAsync(string steamId, string email)
        {
            var user = await GetUserBySteamIdAsync(steamId);
            if (user != null)
            {
                user.Email = email;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task UpdateUserSteamApiKeyAsync(string steamId, string steamApiKey)
        {
            var user = await GetUserBySteamIdAsync(steamId);
            if (user != null)
            {
                user.SteamApiKey = steamApiKey;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task UpdateUserTradeLinkAsync(string steamId, string tradeLink)
        {
            var user = await GetUserBySteamIdAsync(steamId);
            if (user != null)
            {
                user.TradeLink = tradeLink;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> VerifyUserSellerEligibilityAsync(User user)
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

            return await _steamApiService.IsSteamUserAbleToTrade(user);
        }
    }
} 