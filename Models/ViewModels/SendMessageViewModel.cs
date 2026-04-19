using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class SendMessageViewModel
    {
        [Required]
        public int ReceiverId { get; set; }

        [Required]
        public string ReceiverName { get; set; } = string.Empty;

        [Required]
        public int ConversationId { get; set; }

        [Required]
        [StringLength(5000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 5000 characters")]
        [Display(Name = "Message")]
        public string Content { get; set; } = string.Empty;
    }
}