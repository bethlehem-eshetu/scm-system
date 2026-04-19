using System.Collections.Generic;

namespace SCM_System.Services
{
    public interface IContactDetectionService
    {
        ContactDetectionResult DetectContactInfo(string content);
        bool HasContactInfo(string content);
    }

    public class ContactDetectionResult
    {
        public bool HasContactInfo { get; set; }
        public List<string> DetectedPatterns { get; set; } = new();
        public string BlockedReason { get; set; } = string.Empty;
    }
}