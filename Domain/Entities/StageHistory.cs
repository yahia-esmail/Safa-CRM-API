using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// E-2 — Records every stage change for a company, forming a timeline.
/// </summary>
public class StageHistory : IMustHaveTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string FromStage { get; set; } = string.Empty;
    public string ToStage { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    // Navigation
    public Company Company { get; set; } = null!;
    public SystemUser ChangedBy { get; set; } = null!;
}
