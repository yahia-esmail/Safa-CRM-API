using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Services.Notifications;

/// <summary>
/// B-4 — Concrete implementation of INotificationService.
/// Persists notification logs to the database for pull-based frontend queries.
/// </summary>
public class NotificationService(IAppDbContext dbContext) : INotificationService
{
    public async Task SendAsync(
        Guid userId,
        string title,
        string body,
        NotificationType type,
        string? entityType = null,
        string? entityId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();
    }

    public async Task SendToManyAsync(
        IEnumerable<Guid> userIds,
        string title,
        string body,
        NotificationType type,
        string? entityType = null,
        string? entityId = null)
    {
        var now = DateTime.UtcNow;
        var notifications = userIds.Select(userId => new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = now
        }).ToList();

        if (notifications.Any())
        {
            dbContext.Notifications.AddRange(notifications);
            await dbContext.SaveChangesAsync();
        }
    }
}
