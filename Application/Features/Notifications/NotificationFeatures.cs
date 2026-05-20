using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Notifications;

// ── DTOs ────────────────────────────────────────────────────────────────────────

public record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    string Type,
    string? EntityType,
    string? EntityId,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

// ── Queries ──────────────────────────────────────────────────────────────────────

public record GetNotificationsQuery(
    bool? IsRead = null,
    string? Type = null,
    int Page = 1,
    int Size = 20) : IRequest<GetNotificationsResult>;

public record GetNotificationsResult(
    IEnumerable<NotificationDto> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPrevPage);

public record GetUnreadCountQuery : IRequest<int>;

// ── Commands ────────────────────────────────────────────────────────────────────

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest;
public record MarkAllNotificationsReadCommand : IRequest;
public record DeleteNotificationCommand(Guid NotificationId) : IRequest;

// ── Handlers ────────────────────────────────────────────────────────────────────

public class GetNotificationsHandler(IAppDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationsQuery, GetNotificationsResult>
{
    public async Task<GetNotificationsResult> Handle(GetNotificationsQuery q, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var query = context.Notifications
            .Where(n => n.UserId == userId);

        if (q.IsRead.HasValue)
            query = query.Where(n => n.IsRead == q.IsRead.Value);

        if (!string.IsNullOrWhiteSpace(q.Type) && Enum.TryParse<NotificationType>(q.Type, true, out var type))
            query = query.Where(n => n.Type == type);

        var total = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling((double)total / q.Size);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((q.Page - 1) * q.Size)
            .Take(q.Size)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Body, n.Type.ToString(),
                n.EntityType, n.EntityId, n.IsRead, n.CreatedAt, n.ReadAt))
            .ToListAsync(ct);

        return new GetNotificationsResult(
            items, q.Page, q.Size, total, totalPages,
            q.Page < totalPages, q.Page > 1);
    }
}

public class GetUnreadCountHandler(IAppDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetUnreadCountQuery, int>
{
    public async Task<int> Handle(GetUnreadCountQuery q, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }
}

public class MarkNotificationReadHandler(IAppDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == cmd.NotificationId && n.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Notification not found.");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }
}

public class MarkAllNotificationsReadHandler(IAppDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<MarkAllNotificationsReadCommand>
{
    public async Task Handle(MarkAllNotificationsReadCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var unread = await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        await context.SaveChangesAsync(ct);
    }
}

public class DeleteNotificationHandler(IAppDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<DeleteNotificationCommand>
{
    public async Task Handle(DeleteNotificationCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == cmd.NotificationId && n.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Notification not found.");

        context.Notifications.Remove(notification);
        await context.SaveChangesAsync(ct);
    }
}
