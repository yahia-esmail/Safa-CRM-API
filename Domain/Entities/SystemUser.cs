using Domain.Enums;

namespace Domain.Entities;

public class SystemUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AvatarUrl { get; set; }              // C-2 — Profile picture URL
    public string? Phone { get; set; }                  // C-1 — Phone number
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // SAAS Multi-Tenancy
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    // SMTP Settings
    public UserSmtpSetting? SmtpSetting { get; set; }

    // Navigation
    public ICollection<Company> AssignedCompanies { get; set; } = [];
    public ICollection<SalesOrder> SalesOrders { get; set; } = [];
    public ICollection<ImportLog> ImportLogs { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];  // B-1

    // Refresh Token support
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    // Password Reset Token support
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
}
