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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Company> AssignedCompanies { get; set; } = [];
    public ICollection<SalesOrder> SalesOrders { get; set; } = [];
    public ICollection<ImportLog> ImportLogs { get; set; } = [];

    // Refresh Token support
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // Password Reset Token support
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
}
