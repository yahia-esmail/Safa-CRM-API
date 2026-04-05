using Domain.Enums;

namespace Domain.Entities;

public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public ActivityType Type { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Company Company { get; set; } = null!;
    public SystemUser CreatedBy { get; set; } = null!;
}
