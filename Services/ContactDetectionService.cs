using System.Text.RegularExpressions;

namespace SCM_System.Services
{
    public class ContactDetectionService : IContactDetectionService
    {
        private readonly List<DetectionPattern> _patterns;

        public ContactDetectionService()
        {
            _patterns = new List<DetectionPattern>
            {
                // Email patterns
                new DetectionPattern
                {
                    Name = "Email",
                    Pattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
                    Description = "Email addresses are not allowed"
                },
                
                // Ethiopian phone numbers (09 and 07 prefixes) - FIXED
                new DetectionPattern
                {
                    Name = "EthiopianPhone",
                    Pattern = @"\b(09|07|(\+251))[0-9]{8}\b",
                    Description = "Phone numbers are not allowed"
                },
                
                // International/General phone numbers (10-15 digits)
                new DetectionPattern
                {
                    Name = "PhoneNumber",
                    Pattern = @"\b\d{10,15}\b",
                    Description = "Phone numbers are not allowed"
                },
                
                // Phone numbers with separators (e.g., 0912-34-5678, 0912 34 5678)
                new DetectionPattern
                {
                    Name = "PhoneWithSeparators",
                    Pattern = @"\b\d{3,4}[-.\s]?\d{3}[-.\s]?\d{3,4}\b",
                    Description = "Phone numbers are not allowed"
                },
                
                // Telegram
                new DetectionPattern
                {
                    Name = "Telegram",
                    Pattern = @"t\.me\/[a-zA-Z0-9_]+|telegram\.me\/[a-zA-Z0-9_]+|@[a-zA-Z0-9_]{5,}",
                    Description = "Telegram contacts are not allowed"
                },
                
                // WhatsApp
                new DetectionPattern
                {
                    Name = "WhatsApp",
                    Pattern = @"wa\.me\/\d+|whatsapp\.com\/\w+|whatsapp:\/\/",
                    Description = "WhatsApp contacts are not allowed"
                },
                
                // Social media handles
                new DetectionPattern
                {
                    Name = "SocialMedia",
                    Pattern = @"@[a-zA-Z0-9_]{3,}|facebook\.com\/|instagram\.com\/|twitter\.com\/",
                    Description = "Social media contacts are not allowed"
                }
            };
        }

        public ContactDetectionResult DetectContactInfo(string content)
        {
            var result = new ContactDetectionResult();

            if (string.IsNullOrWhiteSpace(content))
                return result;

            var uniqueDescriptions = new HashSet<string>();

            foreach (var pattern in _patterns)
            {
                if (Regex.IsMatch(content, pattern.Pattern, RegexOptions.IgnoreCase))
                {
                    result.HasContactInfo = true;
                    result.DetectedPatterns.Add(pattern.Name);
                    uniqueDescriptions.Add(pattern.Description);
                }
            }

            if (result.HasContactInfo)
            {
                result.BlockedReason = string.Join(" and ", uniqueDescriptions.Select(d => d.ToLower()));
                // Capitalize first letter of reason
                if (!string.IsNullOrEmpty(result.BlockedReason))
                {
                    result.BlockedReason = char.ToUpper(result.BlockedReason[0]) + result.BlockedReason.Substring(1);
                }
            }

            return result;
        }

        public bool HasContactInfo(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            return _patterns.Any(pattern =>
                Regex.IsMatch(content, pattern.Pattern, RegexOptions.IgnoreCase));
        }
    }

    public class DetectionPattern
    {
        public string Name { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}