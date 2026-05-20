using Domain.Enums;

namespace Application.Common.Interfaces;

/// <summary>
/// B-4 — Internal notification service. Injected into Handlers to trigger notifications.
/// Pull-based (no SignalR) — frontend polls /api/notifications/unread-count every 30 seconds.
/// </summary>
public interface INotificationService
{
    /// <summary>Send a notification to a single user.</summary>
    Task SendAsync(
        Guid userId,
        string title,
        string body,
        NotificationType type,
        string? entityType = null,
        string? entityId = null);

    /// <summary>Send the same notification to multiple users.</summary>
    Task SendToManyAsync(
        IEnumerable<Guid> userIds,
        string title,
        string body,
        NotificationType type,
        string? entityType = null,
        string? entityId = null);
}
