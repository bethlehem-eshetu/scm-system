using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class EmployeeViewModel
    {
        public int? Id { get; set; }

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

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }
    }
}
