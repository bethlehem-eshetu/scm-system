using Microsoft.EntityFrameworkCore;
using SCM_System.Data;

namespace SCM_System.Services
{
    public class TenderExpiryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TenderExpiryService> _logger;

        public TenderExpiryService(IServiceScopeFactory scopeFactory, ILogger<TenderExpiryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Tender Expiry Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        
                        var expiredTenders = await context.Tenders
                            .Where(t => t.SubmissionDeadline < DateTime.Now && t.Status == "Published")
                            .ToListAsync(stoppingToken);

                        if (expiredTenders.Any())
                        {
                            _logger.LogInformation($"Found {expiredTenders.Count} expired tenders. Closing them...");
                            foreach (var tender in expiredTenders)
                            {
                                tender.Status = "Closed";
                                tender.UpdatedAt = DateTime.Now;
                            }
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while closing expired tenders.");
                }

                // Check every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            _logger.LogInformation("Tender Expiry Service is stopping.");
        }
    }
}
