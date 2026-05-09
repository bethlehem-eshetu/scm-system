using System.Collections.Generic;

namespace SCM_System.Models.ViewModels
{
    public class SuspendUserRequest
    {
        public int UserId { get; set; }
        public string Reason { get; set; }
    }

    public class VerifyUserRequest
    {
        public int UserId { get; set; }
    }

    public class RejectUserRequest
    {
        public int UserId { get; set; }
        public string Reason { get; set; }
        public bool SendEmail { get; set; }
    }

    public class BulkActionRequest
    {
        public List<int> UserIds { get; set; }
        public string Reason { get; set; }
    }

}
