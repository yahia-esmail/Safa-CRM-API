using MediatR;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Audit;

public record AuditLogDto(
    Guid Id,
    string EntityName,
    string EntityId,
    string Action,
    string OldValues,
    string NewValues,
    Guid? UserId,
    string? UserName,
    string? IpAddress,
    DateTime CreatedAt);

public record GetAuditLogsQuery(
    string? Entity,
    string? EntityId,
    Guid? UserId,
    string? Action,
    DateTime? From,
    DateTime? To,
    Guid CurrentUserId,
    bool IsAdmin) : IRequest<IEnumerable<AuditLogDto>>;

public class GetAuditLogsHandler(IAppDbContext context) : IRequestHandler<GetAuditLogsQuery, IEnumerable<AuditLogDto>>
{
    public async Task<IEnumerable<AuditLogDto>> Handle(GetAuditLogsQuery q, CancellationToken ct)
    {
        if (!q.IsAdmin)
            throw new UnauthorizedAccessException("Only administrators can view audit logs.");

        var query = context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Entity))
            query = query.Where(a => a.EntityName == q.Entity);

        if (!string.IsNullOrWhiteSpace(q.EntityId))
            query = query.Where(a => a.EntityId == q.EntityId);

        if (q.UserId.HasValue)
            query = query.Where(a => a.UserId == q.UserId.Value);

        if (!string.IsNullOrWhiteSpace(q.Action))
            query = query.Where(a => a.Action == q.Action);

        if (q.From.HasValue)
            query = query.Where(a => a.CreatedAt >= q.From.Value);

        if (q.To.HasValue)
            query = query.Where(a => a.CreatedAt <= q.To.Value);

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(1000)
            .ToListAsync(ct);

        return logs.Select(a => new AuditLogDto(
            a.Id,
            a.EntityName,
            a.EntityId,
            a.Action,
            a.OldValues,
            a.NewValues,
            a.UserId,
            a.UserName,
            a.IpAddress,
            a.CreatedAt
        ));
    }
}
