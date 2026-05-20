using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace Infrastructure.Persistence.Interceptors;

public class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        // Automatically update the UpdatedAt property if it exists on modified entities
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null)
                {
                    updatedAtProp.CurrentValue = DateTime.UtcNow;
                }
            }
        }

        if (entries.Count == 0) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var httpContext = httpContextAccessor.HttpContext;
        var userIdString = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? httpContext?.User.FindFirstValue("sub");
        var userId = Guid.TryParse(userIdString, out var parsedId) ? parsedId : (Guid?)null;
        var userName = httpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "System";
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var auditLogs = new List<AuditLog>();

        foreach (var entry in entries)
        {
            // Skip AuditLog and RefreshToken entities
            if (entry.Entity is AuditLog || entry.Entity is RefreshToken) continue;

            var entityName = entry.Entity.GetType().Name;
            
            // Get Primary Key
            var primaryKey = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            var entityId = primaryKey?.CurrentValue?.ToString() ?? "Unknown";

            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = entry.State.ToString(),
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary) continue;

                string propertyName = property.Metadata.Name;

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        oldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            oldValues[propertyName] = property.OriginalValue;
                            newValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }

            auditLog.OldValues = oldValues.Count == 0 ? "{}" : JsonSerializer.Serialize(oldValues);
            auditLog.NewValues = newValues.Count == 0 ? "{}" : JsonSerializer.Serialize(newValues);

            auditLogs.Add(auditLog);
        }

        if (auditLogs.Any())
        {
            await context.Set<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
