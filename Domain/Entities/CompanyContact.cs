using Domain.Common;

namespace Domain.Entities;

public class CompanyContact : IMustHaveTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string? Name { get; set; } 
    public string? Email { get; set; }
    public string? Phone { get; set; }    // E.164 format
    public string? JobTitle { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    // Navigation
    public Company Company { get; set; } = null!;
}
