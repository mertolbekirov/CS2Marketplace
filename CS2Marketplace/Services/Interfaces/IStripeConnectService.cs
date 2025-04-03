using System.Threading.Tasks;
using CS2Marketplace.Models;

namespace CS2Marketplace.Services.Interfaces
{
    public interface IStripeConnectService
    {
        Task<(bool success, string redirectUrl, string errorMessage)> HandleOnboard(User user);
        Task<(bool success, string message)> HandleOnboardingComplete(string stripeConnectAccountId);
        Task<string> CreateOnboardingRefreshLink(User user);
    }
} 