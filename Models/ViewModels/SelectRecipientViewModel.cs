using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class SelectRecipientViewModel
    {
        public int UserId { get; set; }

        [Display(Name = "Business Name")]
        public string BusinessName { get; set; } = string.Empty;

        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public bool IsVerified { get; set; }

        public string Initial => BusinessName.Length > 0 ? BusinessName.Substring(0, 1).ToUpper() : "?";
    }
}