using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SCM_System.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SCM_System.Services
{
    public class ReservationExpiryBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ReservationExpiryBackgroundService> _logger;

        public ReservationExpiryBackgroundService(IServiceProvider services, ILogger<ReservationExpiryBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    
                    var expiredReservations = await inventoryService.GetExpiredReservationsAsync();
                    foreach (var reservation in expiredReservations)
                    {
                        await inventoryService.ReleaseReservationAsync(reservation.Id, "Reservation expired automatically");
                        
                        if (reservation.PurchaseOrderId.HasValue)
                        {
                            var po = await context.PurchaseOrders.FindAsync(reservation.PurchaseOrderId.Value);
                            if (po != null && po.Status != "Cancelled")
                            {
                                po.Status = "Cancelled";
                                po.CancellationReason = "Reservation expired - no order generated within 24 hours";
                            }
                        }

                        if (reservation.OrderId.HasValue)
                        {
                            var order = await context.Orders.FindAsync(reservation.OrderId.Value);
                            if (order != null && order.OrderStatus != "Cancelled" && order.OrderStatus != "Picked")
                            {
                                order.OrderStatus = "Cancelled";
                                order.CancellationReason = "Reservation expired";
                            }
                        }

                        await context.SaveChangesAsync();
                        await notificationService.SendReservationExpiredNotification(reservation);
                    }
                    
                    if (expiredReservations.Count > 0)
                    {
                        _logger.LogInformation($"Processed {expiredReservations.Count} expired reservations");
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
