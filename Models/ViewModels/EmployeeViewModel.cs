using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class EmployeeViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } // "Warehouse" or "Delivery"

        [Display(Name = "Assigned Warehouse")]
        public int? WarehouseId { get; set; }

        [Display(Name = "Assigned Vehicle")]
        public int? VehicleId { get; set; }

        // Personal Information
        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "National ID")]
        public string? NationalID { get; set; }

        // Employment Details
        [Display(Name = "Employee ID (Display)")]
        public string? EmployeeDisplayId { get; set; }

        public string? Department { get; set; } = "Logistics";
        
        [Display(Name = "Employment Type")]
        public SCM_System.Models.Enums.EmploymentType EmploymentType { get; set; } = SCM_System.Models.Enums.EmploymentType.FullTime;

        [Display(Name = "Join Date")]
        [DataType(DataType.Date)]
        public DateTime? JoinDate { get; set; } = DateTime.Now;

        [Display(Name = "Shift")]
        public SCM_System.Models.Enums.ShiftType Shift { get; set; } = SCM_System.Models.Enums.ShiftType.Day;

        [Display(Name = "Salary (Gross)")]
        public decimal? MonthlySalary { get; set; }

        [Display(Name = "Emergency Contact Name")]
        public string? EmergencyContactName { get; set; }

        [Display(Name = "Emergency Contact Phone")]
        public string? EmergencyContactPhone { get; set; }

        // Compliance & Security
        public bool FaydaVerified { get; set; }
        public int SecurityLevel { get; set; } = 1;

        // Security
        [Display(Name = "Force Password Change")]
        public bool ForcePasswordChange { get; set; }

        // Driver License Information
        [Display(Name = "Driving License Number")]
        public string? DrivingLicenseNumber { get; set; }

        [Display(Name = "License Type")]
        public SCM_System.Models.Enums.LicenseType? LicenseType { get; set; }

        [Display(Name = "License Issue Date")]
        [DataType(DataType.Date)]
        public DateTime? LicenseIssueDate { get; set; }

        [Display(Name = "License Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? LicenseExpiryDate { get; set; }

        [Display(Name = "Medical Fitness Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? MedicalFitnessExpiryDate { get; set; }

        // Coverage Area
        [Display(Name = "Primary Delivery Region")]
        public string? DeliveryRegion { get; set; }

        [Display(Name = "City Coverage")]
        public string? CityCoverage { get; set; }

        [Display(Name = "Detailed Coverage Area")]
        public string? CoverageArea { get; set; }

        // Permissions (Warehouse Manager)
        public bool CanApproveTransfers { get; set; } = true;
        public bool CanManageInventory { get; set; } = true;
        public bool CanViewReports { get; set; } = true;

        // Status
        public SCM_System.Models.Enums.EmployeeStatus Status { get; set; } = SCM_System.Models.Enums.EmployeeStatus.Active;

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        // Documents for Display
        public string? ProfilePhotoPath { get; set; }
        public string? IdDocumentUrl { get; set; }
        public string? ContractDocumentUrl { get; set; }
    }
}
