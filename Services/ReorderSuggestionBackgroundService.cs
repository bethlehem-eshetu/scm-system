using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SCM_System.Models.Enums;
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
    public class ReorderSuggestionBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ReorderSuggestionBackgroundService> _logger;

        public ReorderSuggestionBackgroundService(IServiceProvider services, ILogger<ReorderSuggestionBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reorder Suggestion Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                        _logger.LogInformation("Checking for low stock products...");

                        var allProducts = await context.Products
                            .Include(p => p.Supplier)
                            .Include(p => p.Inventories)
                            .Where(p => p.ReorderLevel.HasValue && p.IsAvailable && !p.IsDeleted)
                            .ToListAsync(stoppingToken);

                        // Single source of truth: filter using Inventory-derived available stock
                        var lowStockProducts = allProducts
                            .Where(p => p.Inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved) <= p.ReorderLevel.Value)
                            .ToList();

                        foreach (var product in lowStockProducts)
                        {
                            var available = product.Inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved);
                            await notificationService.SendNotificationAsync(
                                product.Supplier.UserId,
                                "Low Stock Alert ⚠️",
                                $"Product '{product.ProductName}' (SKU: {product.SKU}) has reached the reorder level. Available: {available}, Reorder Level: {product.ReorderLevel}.",
                                "Warning",
                                $"/Supplier/Inventory"
                            );
                        }

                        if (lowStockProducts.Any())
                        {
                            _logger.LogInformation($"Sent {lowStockProducts.Count} reorder suggestions.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing reorder suggestions.");
                }

                // Check every 6 hours
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
    }
}
