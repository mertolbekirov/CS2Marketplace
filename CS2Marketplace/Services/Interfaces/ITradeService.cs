using System.Collections.Generic;
using System.Threading.Tasks;
using CS2Marketplace.Models;

namespace CS2Marketplace.Services.Interfaces
{
    public interface ITradeService
    {
        Task<IEnumerable<Trade>> GetUserTradesAsync(string steamId);
        Task<Trade> GetTradeDetailsAsync(int tradeId, string steamId);
        Task<(bool success, string tradeLink, string message)> CreateTradeOfferAsync(int tradeId, string sellerSteamId);
        Task<(bool success, string message)> MarkTradeAsSentAsync(int tradeId, string sellerSteamId);
        Task<(bool success, string message)> ConfirmTradeReceiptAsync(int tradeId, string buyerSteamId);
        Task<(bool success, string message)> DisputeTradeAsync(int tradeId, string buyerSteamId, string reason);
    }
} 