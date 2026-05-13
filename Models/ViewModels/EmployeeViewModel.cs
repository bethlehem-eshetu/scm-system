using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class EmployeeViewModel
    {
        // ========== IDENTITY & ID ==========
        public int Id { get; set; }  // ← ADD THIS - CRITICAL FOR EDIT

        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "DeliveryAgent"; // "WarehouseManager" or "DeliveryAgent"

        [Display(Name = "Assigned Warehouse")]
        public int? WarehouseId { get; set; }

        [Display(Name = "Assigned Vehicle")]
        public int? VehicleId { get; set; }

        // ========== PERSONAL INFORMATION ==========
        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "National ID")]
        public string? NationalID { get; set; }

        // ========== EMPLOYMENT DETAILS ==========
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

        [Display(Name = "Current Status")]
        public SCM_System.Models.Enums.EmployeeStatus Status { get; set; } = SCM_System.Models.Enums.EmployeeStatus.Active;

        [Display(Name = "Monthly Salary (ETB)")]
        public decimal? MonthlySalary { get; set; }

        [Display(Name = "Emergency Contact Name")]
        public string? EmergencyContactName { get; set; }

        [Display(Name = "Emergency Contact Phone")]
        public string? EmergencyContactPhone { get; set; }

        // ========== SECURITY & COMPLIANCE ==========
        public bool FaydaVerified { get; set; }
        public int SecurityLevel { get; set; } = 1;

        [Display(Name = "Force password change on next login")]
        public bool ForcePasswordChange { get; set; } = false;

        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        // ========== DRIVER LICENSE INFORMATION ==========
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

        [Display(Name = "Medical Fitness Expiry")]
        [DataType(DataType.Date)]
        public DateTime? MedicalFitnessExpiryDate { get; set; }

        // ========== DELIVERY COVERAGE ==========
        [Display(Name = "Primary Delivery Region")]
        public string? DeliveryRegion { get; set; }

        [Display(Name = "City Coverage")]
        public string? CityCoverage { get; set; }

        [Display(Name = "Detailed Coverage Area")]
        public string? CoverageArea { get; set; }

        // ========== WAREHOUSE MANAGER PERMISSIONS ==========
        [Display(Name = "Can Approve Transfers")]
        public bool CanApproveTransfers { get; set; } = true;

        [Display(Name = "Can Manage Inventory")]
        public bool CanManageInventory { get; set; } = true;

        [Display(Name = "Can View Reports")]
        public bool CanViewReports { get; set; } = true;

        // ========== DOCUMENT PATHS (FOR DISPLAY) ==========
        public string? ProfilePhotoPath { get; set; }
        public string? IdDocumentUrl { get; set; }
        public string? ContractDocumentUrl { get; set; }
    }
}