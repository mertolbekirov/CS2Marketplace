using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CS2Marketplace.Services
{
    public class TradeStatusCheckService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<TradeStatusCheckService> _logger;

        public TradeStatusCheckService(
            IServiceProvider services,
            ILogger<TradeStatusCheckService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var tradeService = scope.ServiceProvider.GetRequiredService<SteamTradeService>();
                        await tradeService.CheckPendingTrades();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking trade status");
                }

                // Check every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
} 