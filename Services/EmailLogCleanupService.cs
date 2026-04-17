using Microsoft.EntityFrameworkCore;
using SCM_System.Data;

namespace SCM_System.Services
{
    public class EmailLogCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailLogCleanupService> _logger;

        public EmailLogCleanupService(IServiceProvider serviceProvider, ILogger<EmailLogCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("EmailLogCleanupService is running.");

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var cutoffDate = DateTime.Now.AddDays(-90);

                        var logsToDelete = await context.EmailLogs
                            .Where(l => l.SentAt < cutoffDate)
                            .ToListAsync(stoppingToken);

                        if (logsToDelete.Any())
                        {
                            _logger.LogInformation("Deleting {Count} email logs older than 90 days.", logsToDelete.Count);
                            context.EmailLogs.RemoveRange(logsToDelete);
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up email logs.");
                }

                // Run once every 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
