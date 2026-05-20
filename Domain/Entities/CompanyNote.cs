using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// D-6 — A free-form text note attached to a company, separate from Activities.
/// Notes = free text. Activities = structured events with a type.
/// </summary>
public class CompanyNote : IMustHaveTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    // Navigation
    public Company Company { get; set; } = null!;
    public SystemUser CreatedBy { get; set; } = null!;
}
