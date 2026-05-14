using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SCM_System.Models.Enums;

namespace SCM_System.Services
{
    public class PaymentExpiryBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentExpiryBackgroundService> _logger;

        public PaymentExpiryBackgroundService(IServiceProvider serviceProvider, ILogger<PaymentExpiryBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredPaymentsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired payments");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessExpiredPaymentsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var expiryThreshold = DateTime.Now.AddHours(-24);

            var expiredPayments = await context.Commissions
                .Where(c => c.Status == "Pending" && c.CreatedAt < expiryThreshold)
                .ToListAsync(stoppingToken);

            if (expiredPayments.Any())
            {
                foreach (var payment in expiredPayments)
                {
                    payment.Status = "Expired";
                    var order = await context.Orders.FindAsync(new object[] { payment.OrderId }, stoppingToken);
                    if (order != null && order.OrderStatus != "Delivered")
                    {
                        order.OrderStatus = "Cancelled";
                        order.RejectionReason = "Payment timeout";
                        order.RejectedAt = DateTime.Now;

                        // Notify Retailer
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        var retailer = await context.Retailers.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == order.RetailerId, stoppingToken);

                        if (retailer?.User != null)
                        {
                            await notificationService.SendNotificationAsync(
                                retailer.User.Id,
                                "Order Cancelled - Payment Timeout",
                                $"Order #{order.OrderNumber} has been cancelled due to payment expiry.",
                                "Warning",
                                "/Payment/MyPayments"
                            );

                            await emailService.SendPaymentExpiryEmailAsync(
                                retailer.User.Email,
                                retailer.BusinessName,
                                order.OrderNumber);
                        }
                    }
                }
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"Processed {expiredPayments.Count} expired payments.");
            }
        }
    }
}
