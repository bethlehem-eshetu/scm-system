namespace SCM_System.Models.ViewModels
{
    public class PendingUsersViewModel
    {
        public int TotalPendingCount { get; set; }
        public int VerifiedTodayCount { get; set; }
        public int RejectedTodayCount { get; set; }
        public int WaitlistCount { get; set; }
        public double AverageApprovalTimeHours { get; set; }
        public List<PendingUserCard> PendingUsers { get; set; } = new List<PendingUserCard>();
    }

    public class PendingUserCard
    {
        public int UserId { get; set; } // Renamed from Id to UserId
        public string FullName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Renamed from UserType to Role
        public DateTime AppliedAt { get; set; } // Renamed from AppliedDate to AppliedAt
        public string Status { get; set; } = "PendingApproval";
        public string? FaydaId { get; set; }
        public bool IsFaydaVerified { get; set; } // Added
        public int ConfidenceScore { get; set; } // Renamed from FaydaConfidenceScore
        public List<string> Documents { get; set; } = new List<string>();
        public string RiskScore { get; set; } = "Low"; // Renamed from RiskLevel to RiskScore (string to match .ToLower() in view)
        public string? TaxId { get; set; }
        public List<string> RiskFactors { get; set; } = new List<string>();
    }
}
