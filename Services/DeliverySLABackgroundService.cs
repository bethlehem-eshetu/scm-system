using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SCM_System.Data;
using SCM_System.Models.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCM_System.Services
{
    public class DeliverySLABackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DeliverySLABackgroundService> _logger;

        public DeliverySLABackgroundService(IServiceProvider services, ILogger<DeliverySLABackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Delivery SLA Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                        _logger.LogInformation("Checking for delayed deliveries...");

                        var delayedOrders = await context.Orders
                            .Include(o => o.Supplier)
                            .Include(o => o.Retailer)
                            .Where(o => o.ExpectedDeliveryDate.HasValue && 
                                        o.ExpectedDeliveryDate.Value < DateTime.Now && 
                                        o.OrderStatus != "Completed" && 
                                        o.OrderStatus != "Cancelled" && 
                                        o.OrderStatus != "Rejected" &&
                                        o.OrderStatus != "Delivered")
                            .ToListAsync(stoppingToken);

                        foreach (var order in delayedOrders)
                        {
                            // Notify Supplier
                            await notificationService.SendNotificationAsync(
                                order.Supplier.UserId,
                                "Delivery Delay Alert ⏳",
                                $"Order {order.OrderNumber} is past its expected delivery date: {order.ExpectedDeliveryDate?.ToString("f")}. Please update the status.",
                                "Danger",
                                $"/Supplier/Orders"
                            );

                            // Notify Retailer
                            await notificationService.SendNotificationAsync(
                                order.Retailer.UserId,
                                "Order Delayed Update 📦",
                                $"Order {order.OrderNumber} is experiencing a delay. Expected date was: {order.ExpectedDeliveryDate?.ToString("d")}. We are working on it.",
                                "Warning",
                                $"/Retailer/OrderDetails/{order.Id}"
                            );
                        }

                        if (delayedOrders.Any())
                        {
                            _logger.LogInformation($"Processed {delayedOrders.Count} delayed orders.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while monitoring delivery SLAs.");
                }

                // Check every 12 hours
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }
    }
}
