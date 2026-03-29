using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
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

        public SupplierService(ApplicationDbContext context)
        {
            _context = context;
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
            
            var viewModel = new SupplierDashboardViewModel
            {
                TotalRevenue = orders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount),
                ActiveOrders = orders.Count(o => new[] { "Pending", "Accepted", "Processing", "Packed", "In Transit" }.Contains(o.OrderStatus)),
                OrdersInDelivery = orders.Count(o => o.OrderStatus == "In Transit"),
                LowStockItems = products.Count(p => p.Inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved) < (p.ReorderLevel ?? 10)),
                CompletionRate = orders.Any() ? (double)orders.Count(o => o.OrderStatus == "Completed") / orders.Count * 100 : 0,

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
                    .Where(w => w.SupplierId == supplierId)
                    .Select(w => new ChartDataPoint { 
                        Label = w.Name, 
                        Value = _context.PurchaseOrders.Count(po => po.WarehouseId == w.Id) 
                    })
                    .ToListAsync(),

                RecentActivity = await _context.Notifications
                    .Where(n => n.UserId == _context.Suppliers.First(s => s.Id == supplierId).UserId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .Select(n => new SupplierActivityItem {
                        Title = n.Title,
                        Description = n.Message,
                        Time = n.CreatedAt,
                        Type = n.Type,
                        ActionUrl = n.ActionUrl
                    })
                    .ToListAsync()
            };

            return viewModel;
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
                            Warehouse = p.Inventories.FirstOrDefault().Warehouse.Name
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
                .Where(c => c.SupplierId == supplierId)
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
    }
}
