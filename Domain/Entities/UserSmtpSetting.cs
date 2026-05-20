namespace Domain.Entities;

public class UserSmtpSetting
{
    public Guid UserId { get; set; }
    public SystemUser? User { get; set; }

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Encryption { get; set; } = "STARTTLS"; // STARTTLS, SSL, None
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
