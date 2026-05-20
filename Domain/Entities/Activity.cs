using Domain.Enums;

using Domain.Common;

namespace Domain.Entities;

public class Activity : IMustHaveTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public ActivityType Type { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // D-1 — New fields
    public DateTime? DueDate { get; set; }         // For Task activities
    public bool IsCompleted { get; set; } = false; // For Task activities
    public DateTime? CompletedAt { get; set; }     // When was the task completed

    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    // Navigation
    public Company Company { get; set; } = null!;
    public SystemUser CreatedBy { get; set; } = null!;
}
