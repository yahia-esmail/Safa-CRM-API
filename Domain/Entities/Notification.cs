using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// B-1 — In-App Notification. Each record is one notification for one user.
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }                   // FK → SystemUser (recipient)
    public string Title { get; set; } = string.Empty;  // "New company assigned to you"
    public string Body { get; set; } = string.Empty;   // Full message body
    public NotificationType Type { get; set; }

    public string? EntityType { get; set; }            // "Company" | "SalesOrder" | "Activity"
    public string? EntityId { get; set; }              // ID of the related record

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    // Navigation
    public SystemUser User { get; set; } = null!;
}
