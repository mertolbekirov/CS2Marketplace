using System.Threading.Tasks;
using CS2Marketplace.Models;

namespace CS2Marketplace.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> GetUserBySteamIdAsync(string steamId);
        Task<User> GetUserWithTransactionsAsync(string steamId);
        Task UpdateUserEmailAsync(string steamId, string email);
        Task UpdateUserSteamApiKeyAsync(string steamId, string steamApiKey);
        Task UpdateUserTradeLinkAsync(string steamId, string tradeLink);
        Task<bool> VerifyUserSellerEligibilityAsync(User user);
        Task<User> GetOrCreateUserAsync(string steamId, string username, string avatarUrl);
        Task<User> GetUserDetailsByIdAsync(int id);
    }
} 