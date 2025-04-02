using System.Threading.Tasks;
using CS2Marketplace.Models;

namespace CS2Marketplace.Services.Interfaces
{
    public interface ISteamVerificationService
    {
        Task<bool> VerifyUserEligibilityAsync(User user);
    }
} 