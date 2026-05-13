using System;
using System.Collections.Generic;

namespace SCM_System.Models.ViewModels
{
    public class DeliveryRatingViewModel
    {
        public int PurchaseOrderId { get; set; }
        public string PONumber { get; set; }
        public string DriverName { get; set; }
        public string VehiclePlate { get; set; }

        public int Timeliness { get; set; }
        public int Professionalism { get; set; }
        public int VehicleCondition { get; set; }
        public int Communication { get; set; }
        public string? Comment { get; set; }
    }

    public class DriverPerformanceViewModel
    {
        public double OverallRating { get; set; }
        public int TotalDeliveries { get; set; }
        public double OnTimeRate { get; set; }
        
        public double AverageTimeliness { get; set; }
        public double AverageProfessionalism { get; set; }
        public double AverageVehicleCondition { get; set; }
        public double AverageCommunication { get; set; }

        public List<RecentRatingViewModel> RecentRatings { get; set; } = new();
        public List<string> MonthlyLabels { get; set; } = new();
        public List<double> MonthlyRatings { get; set; } = new();
        public List<double> MonthlyOnTimeRates { get; set; } = new();
    }

    public class RecentRatingViewModel
    {
        public string PONumber { get; set; }
        public double OverallRating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
