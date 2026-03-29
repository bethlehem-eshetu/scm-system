using System;
using System.Collections.Generic;

namespace SCM_System.Models.ViewModels
{
    public class SupplierDashboardViewModel
    {
        // Top Cards
        public decimal TotalRevenue { get; set; }
        public int ActiveOrders { get; set; }
        public int OrdersInDelivery { get; set; }
        public int LowStockItems { get; set; }
        public double CompletionRate { get; set; }

        // Status Counts
        public Dictionary<string, int> StatusCounts { get; set; }

        // Analytics Data (for charts)
        public List<ChartDataPoint> RevenueTrend { get; set; }
        public List<ChartDataPoint> OrderVolumeTrend { get; set; }
        public List<ChartDataPoint> WarehousePerformance { get; set; }

        // Recent Activity
        public List<SupplierActivityItem> RecentActivity { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }

    public class SupplierActivityItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Time { get; set; }
        public string Type { get; set; } // Info, Success, Warning, Danger
        public string ActionUrl { get; set; }
    }

    public class SupplierReportsViewModel
    {
        public WarehousePerformanceReport WarehousePerformance { get; set; }
        public InventoryReport Inventory { get; set; }
        public DeliveryPerformanceReport Delivery { get; set; }
        public FinancialReport Finance { get; set; }
    }

    public class WarehousePerformanceReport
    {
        public int TotalOrdersProcessed { get; set; }
        public int TotalItemsPacked { get; set; }
        public double AvgProcessingTimeHours { get; set; }
        public List<WarehouseStat> WarehouseStats { get; set; }
    }

    public class WarehouseStat
    {
        public string WarehouseName { get; set; }
        public int Orders { get; set; }
        public double Efficiency { get; set; }
    }

    public class InventoryReport
    {
        public int TotalProducts { get; set; }
        public int LowStockAlerts { get; set; }
        public int OutOfStockItems { get; set; }
        public List<InventoryItemDetail> TopLowStockItems { get; set; }
    }

    public class InventoryItemDetail
    {
        public string ProductName { get; set; }
        public int Stock { get; set; }
        public int Reserved { get; set; }
        public string Warehouse { get; set; }
    }

    public class DeliveryPerformanceReport
    {
        public double OnTimeRate { get; set; }
        public int DelayedDeliveries { get; set; }
        public int CompletedDeliveries { get; set; }
        public List<DeliveryAgentStat> AgentStats { get; set; }
    }

    public class DeliveryAgentStat
    {
        public string AgentName { get; set; }
        public int CompletedDeliveries { get; set; }
        public double Rating { get; set; }
    }

    public class FinancialReport
    {
        public decimal TotalRevenue { get; set; }
        public decimal PendingPayments { get; set; }
        public decimal CommissionDue { get; set; }
        public List<MonthlyRevenue> RevenueHistory { get; set; }
    }

    public class MonthlyRevenue
    {
        public string Month { get; set; }
        public decimal Revenue { get; set; }
    }
}
