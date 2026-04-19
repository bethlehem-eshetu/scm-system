namespace SCM_System.Models
{
    public class EmailSettings
    {
        public bool EnableEmail { get; set; } = true;
        public string SenderName { get; set; } = "EthioChain SCM";
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool EnableSsl { get; set; } = true;
    }
}
