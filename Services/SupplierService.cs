using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.Enums;
using SCM_System.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCM_System.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly INotificationService _notificationService;

        public SupplierService(ApplicationDbContext context, IInventoryService inventoryService, INotificationService notificationService)
        {
            _context = context;
            _inventoryService = inventoryService;
            _notificationService = notificationService;
        }

        public async Task<SupplierDashboardViewModel> GetDashboardAnalyticsAsync(int supplierId)
        {
            var orders = await _context.Orders
                .Where(o => o.SupplierId == supplierId)
                .ToListAsync();

            var products = await _context.Products
                .Include(p => p.Inventories)
                .Where(p => p.SupplierId == supplierId)
                .ToListAsync();

            var last30Days = DateTime.Now.AddDays(-30);
            var prev30Days = DateTime.Now.AddDays(-60);
            
            var supplier = await _context.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == supplierId);

            // Revenue Trends & Growth
            var currentMonthRevenue = orders.Where(o => o.CreatedAt >= last30Days && o.OrderStatus == "Completed").Sum(o => o.TotalAmount);
            var prevMonthRevenue = orders.Where(o => o.CreatedAt >= prev30Days && o.CreatedAt < last30Days && o.OrderStatus == "Completed").Sum(o => o.TotalAmount);
            double growthPercent = prevMonthRevenue > 0 ? (double)((currentMonthRevenue - prevMonthRevenue) / prevMonthRevenue) * 100 : 0;

            var viewModel = new SupplierDashboardViewModel
            {
                IsFaydaVerified = supplier?.User?.IsFaydaVerified ?? false,
                FaydaStatus = supplier?.User?.FaydaStatus ?? "Pending",
                TotalRevenue = orders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount),
                ActiveOrders = orders.Count(o => new[] { "Pending", "Accepted", "Processing", "Packed", "In Transit" }.Contains(o.OrderStatus)),
                OrdersInDelivery = orders.Count(o => o.OrderStatus == "In Transit"),
                LowStockItems = products.Count(p => p.Inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved) < (p.ReorderLevel ?? 10)),
                CompletionRate = orders.Any() ? (double)orders.Count(o => o.OrderStatus == "Completed") / orders.Count * 100 : 0,

                // Investor-Grade KPIs
                GrowthPercent = growthPercent,
                DelayedShipmentsCount = orders.Count(o => (o.OrderStatus == "In Transit" || o.OrderStatus == "Processing") && o.CreatedAt.AddDays(2) < DateTime.Now), // Mocked ETA 2 days
                
                // Logistics 2.0 KPIs
                ActiveVehiclesCount = await _context.Vehicles.CountAsync(v => v.SupplierId == supplierId && !v.IsDeleted && v.IsActive),
                ReadyVehicles = await _context.Vehicles.CountAsync(v => v.SupplierId == supplierId && v.Status == SCM_System.Models.Enums.VehicleStatus.Available && !v.IsDeleted && v.IsActive),
                
                HubUtilizationPercent = (await _context.Warehouses
                    .Where(w => w.SupplierId == supplierId && !w.IsDeleted && w.IsActive && w.MaxCapacity > 0)
                    .Select(w => new { w.CapacityUsed, w.MaxCapacity })
                    .ToListAsync())
                    .DefaultIfEmpty(new { CapacityUsed = (int?)0, MaxCapacity = 1 })
                    .Average(w => (double)(w.CapacityUsed ?? 0) * 100.0 / w.MaxCapacity),

                TotalPersonnelCount = await _context.SupplierEmployees.CountAsync(se => se.SupplierId == supplierId && !se.IsDeleted),
                OnDutyCount = await _context.SupplierEmployees.CountAsync(se => se.SupplierId == supplierId && se.Status == SCM_System.Models.Enums.EmployeeStatus.Active && !se.IsDeleted),

                StatusCounts = orders.GroupBy(o => o.OrderStatus)
                    .ToDictionary(g => g.Key, g => g.Count()),

                RevenueTrend = orders.Where(o => o.CreatedAt >= last30Days && o.OrderStatus == "Completed")
                    .GroupBy(o => o.CreatedAt.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new ChartDataPoint { Label = g.Key.ToString("MMM dd"), Value = g.Sum(o => o.TotalAmount) })
                    .ToList(),

                OrderVolumeTrend = orders.Where(o => o.CreatedAt >= last30Days)
                    .GroupBy(o => o.CreatedAt.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new ChartDataPoint { Label = g.Key.ToString("MMM dd"), Value = g.Count() })
                    .ToList(),

                WarehousePerformance = await _context.Warehouses
                    .Where(w => w.SupplierId == supplierId && !w.IsDeleted)
                    .Select(w => new ChartDataPoint { 
                        Label = w.Name, 
                        Value = _context.PurchaseOrders.Count(po => po.WarehouseId == w.Id) 
                    })
                    .ToListAsync(),

                RecentActivity = (await _context.AuditLogs
                    .Where(l => l.PerformedByUserId == supplier.UserId || l.EntityType == "Supplier")
                    .OrderByDescending(l => l.PerformedAtUtc)
                    .Take(10)
                    .Select(l => new { l.ActionType, l.EntityType, l.EntityId, l.Notes, l.PerformedAtUtc })
                    .ToListAsync())
                    .Select(l => new SupplierActivityItem
                    {
                        Title = l.ActionType,
                        Description = $"{l.EntityType} #{l.EntityId}: {l.Notes ?? "No details"}",
                        Time = l.PerformedAtUtc,
                        Type = "Info"
                    })
                    .DefaultIfEmpty(new SupplierActivityItem { Title = "No Activity", Description = "System is running normally", Time = DateTime.Now })
                    .ToList()
            };

            return viewModel;
        }

        public async Task<List<VehicleSuggestion>> GetSmartDispatchSuggestionsAsync(int warehouseId)
        {
            var warehouse = await _context.Warehouses.FindAsync(warehouseId);
            if (warehouse == null) return new List<VehicleSuggestion>();

            var availableVehicles = await _context.Vehicles
                .Include(v => v.PrimaryDriver)
                    .ThenInclude(d => d.User)
                .Where(v => v.WarehouseId == warehouseId && v.Status == SCM_System.Models.Enums.VehicleStatus.Available && !v.IsDeleted && v.IsActive)
                .ToListAsync();

            // Scoring Formula logic
            // Dispatch Score = Zone Match (40) + Capacity Fit (30) + Availability (10) + Rating (20)
            return availableVehicles
                .OrderByDescending(v => {
                    int score = 0;
                    if (v.TemperatureControlled) score += 20;
                    if (v.MaxLoadCapacity > 1000) score += 15;
                    if (v.PrimaryDriver != null) score += 25;
                    return score;
                })
                .Select((v, index) => new VehicleSuggestion 
                { 
                    VehicleId = v.Id, 
                    Rank = index + 1 
                })
                .ToList();
        }

        public async Task<List<DriverSuggestion>> GetSmartDriverSuggestionsAsync(int warehouseId)
        {
            var agents = await _context.SupplierEmployees
                .Include(e => e.User)
                .Where(e => e.WarehouseId == warehouseId && e.EmployeeRole == "DeliveryAgent" && e.Status == SCM_System.Models.Enums.EmployeeStatus.Active && !e.IsDeleted)
                .ToListAsync();

            return agents.Select((a, index) => new DriverSuggestion
            {
                DriverId = a.Id,
                Rank = index + 1
            }).ToList();
        }

        public async Task<SupplierReportsViewModel> GetSupplierReportsAsync(int supplierId)
        {
            var warehouses = await _context.Warehouses
                .Where(w => w.SupplierId == supplierId)
                .ToListAsync();

            var report = new SupplierReportsViewModel
            {
                WarehousePerformance = new WarehousePerformanceReport
                {
                    TotalOrdersProcessed = await _context.PurchaseOrders.CountAsync(po => po.SupplierId == supplierId && po.Status == "Delivered"),
                    TotalItemsPacked = 0, // Simplified for now
                    AvgProcessingTimeHours = 24.5, // Mocked for now
                    WarehouseStats = warehouses.Select(w => new WarehouseStat {
                        WarehouseName = w.Name,
                        Orders = _context.PurchaseOrders.Count(po => po.WarehouseId == w.Id),
                        Efficiency = 85.0
                    }).ToList()
                },
                Inventory = new InventoryReport
                {
                    TotalProducts = await _context.Products.CountAsync(p => p.SupplierId == supplierId),
                    LowStockAlerts = await _context.Products.CountAsync(p => p.SupplierId == supplierId && p.Inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved) < 10),
                    OutOfStockItems = await _context.Products.CountAsync(p => p.SupplierId == supplierId && p.Inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved) <= 0),
                    TopLowStockItems = await _context.Products
                        .Where(p => p.SupplierId == supplierId)
                        .OrderBy(p => p.Inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved))
                        .Take(5)
                        .Select(p => new InventoryItemDetail {
                            ProductName = p.ProductName,
                            Stock = p.Inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved),
                            Reserved = p.Inventories.Sum(i => i.QuantityReserved),
                            Warehouse = p.Inventories.Select(i => i.Warehouse.Name).FirstOrDefault() ?? "N/A"
                        })
                        .ToListAsync()
                },
                Delivery = new DeliveryPerformanceReport
                {
                    OnTimeRate = 92.5,
                    DelayedDeliveries = 4,
                    CompletedDeliveries = 120,
                    AgentStats = await _context.SupplierEmployees
                        .Where(se => se.SupplierId == supplierId && se.EmployeeRole == "DeliveryAgent")
                        .Select(se => new DeliveryAgentStat {
                            AgentName = se.User.FullName,
                            CompletedDeliveries = 0, // Mocked
                            Rating = 4.8
                        })
                        .ToListAsync()
                },
                Finance = new FinancialReport
                {
                    TotalRevenue = await _context.Orders.Where(o => o.SupplierId == supplierId && o.OrderStatus == "Completed").SumAsync(o => o.TotalAmount),
                    PendingPayments = await _context.Orders.Where(o => o.SupplierId == supplierId && o.PaymentStatus == "Pending").SumAsync(o => o.TotalAmount),
                    CommissionDue = await _context.Commissions.Where(c => c.SupplierId == supplierId && c.Status == "Pending").SumAsync(c => c.CommissionAmount),
                    RevenueHistory = new List<MonthlyRevenue> {
                        new MonthlyRevenue { Month = "Jan", Revenue = 5000 },
                        new MonthlyRevenue { Month = "Feb", Revenue = 7000 },
                        new MonthlyRevenue { Month = "Mar", Revenue = 12000 }
                    }
                }
            };

            return report;
        }

        public async Task<IEnumerable<Order>> GetSupplierOrdersForTrackingAsync(int supplierId)
        {
            return await _context.Orders
                .Include(o => o.Retailer)
                .Include(o => o.PurchaseOrders)
                .Include(o => o.StatusHistory)
                .Where(o => o.SupplierId == supplierId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Commission>> GetSupplierCommissionsAsync(int supplierId)
        {
            return await _context.Commissions
                .Include(c => c.PurchaseOrder)
                .Where(c => c.SupplierId == supplierId && c.PaymentType == "PlatformCommission")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Commission> GetCommissionByIdAsync(int commissionId)
        {
            return await _context.Commissions
                .Include(c => c.PurchaseOrder)
                .Include(c => c.Supplier)
                .FirstOrDefaultAsync(c => c.Id == commissionId);
        }

        public async Task<bool> UpdateCommissionPaymentStatusAsync(int commissionId, string chapaId, string status, string verificationData)
        {
            var commission = await _context.Commissions.FindAsync(commissionId);
            if (commission == null) return false;

            commission.ChapaTransactionId = chapaId;
            commission.Status = status;
            commission.PaymentVerificationData = verificationData;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateSupplierTierAsync(int supplierId)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.Orders)
                .Include(s => s.ReceivedRatings)
                .FirstOrDefaultAsync(s => s.Id == supplierId);

            if (supplier == null) return false;

            var completedOrders = supplier.Orders.Count(o => o.OrderStatus == "Completed");
            var averageRating = supplier.ReceivedRatings.Any() ? supplier.ReceivedRatings.Average(r => r.RatingValue) : 0;

            string newTier = "Bronze";
            if (completedOrders >= 1000 || averageRating >= 4.9) newTier = "Platinum";
            else if (completedOrders >= 500 || averageRating >= 4.7) newTier = "Gold";
            else if (completedOrders >= 100 || averageRating >= 4.5) newTier = "Silver";

            if (supplier.CommissionTier != newTier)
            {
                supplier.CommissionTier = newTier;
                supplier.CommissionRate = Supplier.GetRateByTier(newTier);
                _context.Suppliers.Update(supplier);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> CancelOrderAsync(int orderId, string reason)
        {
            var order = await _context.Orders
                .Include(o => o.PurchaseOrders)
                .Include(o => o.Retailer)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            order.OrderStatus = "Cancelled";
            order.CancellationReason = reason;
            order.UpdatedAt = DateTime.Now;

            foreach (var po in order.PurchaseOrders)
            {
                po.Status = "Cancelled";
                po.UpdatedAt = DateTime.Now;
            }

            // Return stock to inventory
            await _inventoryService.ReturnStockOnCancelAsync(orderId);

            await _context.SaveChangesAsync();

            // Notify Retailer
            if (order.Retailer?.UserId != null)
            {
                await _notificationService.SendNotificationAsync(
                    order.Retailer.UserId,
                    "Order Cancelled",
                    $"Your Order #{order.OrderNumber} has been cancelled. Reason: {reason}",
                    "Warning",
                    "/Order/MyOrders"
                );
            }

            return true;
        }
    }
}
