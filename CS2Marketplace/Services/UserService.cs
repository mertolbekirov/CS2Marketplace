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
        private readonly ISteamVerificationService _steamVerificationService;

        public UserService(ApplicationDbContext dbContext, ISteamVerificationService steamVerificationService)
        {
            _dbContext = dbContext;
            _steamVerificationService = steamVerificationService;
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

        public async Task<bool> VerifyUserEligibilityAsync(User user)
        {
            return await _steamVerificationService.VerifyUserEligibilityAsync(user);
        }
    }
} 