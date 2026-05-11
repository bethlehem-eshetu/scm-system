using System;
using System.Collections.Generic;

namespace SCM_System.Models.ViewModels
{
    public class SupplierDetailsViewModel
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? AccountOwner { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? BusinessCategory { get; set; }
        public string? Headquarters { get; set; }
        public string? DetailedAddress { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime MemberSince { get; set; }
        public string? TaxId { get; set; }
        public string? BusinessLicensePath { get; set; }
        public string? PermitPath { get; set; }
        public DateTime DocumentUploadDate { get; set; }

        // Fayda data
        public bool FaydaVerified { get; set; }
        public string? FaydaId { get; set; }
        public string? FaydaRegistryName { get; set; }
        public DateTime? FaydaDOB { get; set; }
        public int FaydaConfidenceScore { get; set; }

        // Audit history
        public List<AuditLogEntry> AuditHistory { get; set; } = new List<AuditLogEntry>();
    }

    public class AuditLogEntry
    {
        public string Action { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
