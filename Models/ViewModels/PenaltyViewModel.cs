using System;

namespace SCM_System.Models.ViewModels
{
    public class PenaltyViewModel
    {
        public int Id { get; set; }
        public string PenaltyType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }

        public string Status
        {
            get
            {
                if (!IsActive)
                    return "Expired";
                if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.Now)
                    return "Expired";
                return "Active";
            }
        }

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - CreatedAt;

                if (timeSpan.TotalDays < 1)
                    return $"{(int)timeSpan.TotalHours} hours ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} days ago";
                if (timeSpan.TotalDays < 30)
                    return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";

                return CreatedAt.ToString("MMM dd, yyyy");
            }
        }

        public string ExpiryInfo
        {
            get
            {
                if (!ExpiresAt.HasValue)
                    return "No expiry";

                if (ExpiresAt.Value < DateTime.Now)
                    return "Expired";

                var daysLeft = (ExpiresAt.Value - DateTime.Now).Days;
                return $"Expires in {daysLeft} days";
            }
        }
    }
}