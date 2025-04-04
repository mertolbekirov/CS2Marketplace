using System.Collections.Generic;
using System.Threading.Tasks;
using CS2Marketplace.Models;

namespace CS2Marketplace.Services.Interfaces
{
    public interface IAdminService
    {
        Task<IEnumerable<Trade>> GetDisputedTradesAsync();
        Task<Trade> GetDisputeDetailsAsync(int tradeId);
        Task<(bool success, string message)> ResolveDisputeAsync(int tradeId, bool refundBuyer, string resolution, string adminNotes);
        Task<(bool success, string message)> ProcessExpiredTradesAsync();
        Task<(bool success, string message)> MakeUserAdminAsync(string steamId);
    }
} 