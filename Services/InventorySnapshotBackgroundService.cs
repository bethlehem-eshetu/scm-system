using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SCM_System.Services
{
    public class InventorySnapshotBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<InventorySnapshotBackgroundService> _logger;

        public InventorySnapshotBackgroundService(IServiceProvider services, ILogger<InventorySnapshotBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Inventory Snapshot Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                // Calculate time until next midnight
                var nextRunTime = now.Date.AddDays(1);
                var delay = nextRunTime - now;

                _logger.LogInformation($"Next inventory snapshot scheduled for {nextRunTime}. Delay: {delay}");

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
                        _logger.LogInformation("Triggering daily inventory snapshot...");
                        await inventoryService.CreateDailySnapshotAsync();
                        _logger.LogInformation("Daily inventory snapshot completed successfully.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while creating daily inventory snapshot.");
                }
            }
        }
    }
}
