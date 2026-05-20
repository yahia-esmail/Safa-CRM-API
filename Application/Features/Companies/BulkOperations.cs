using MediatR;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Companies;

// Requests
public record BulkAssignRequest(IEnumerable<Guid> CompanyIds, Guid UserId);
public record BulkStageRequest(IEnumerable<Guid> CompanyIds, string Stage);
public record BulkDeleteRequest(IEnumerable<Guid> CompanyIds);

// Commands
public record BulkAssignCommand(BulkAssignRequest Request, Guid CurrentUserId, bool IsAdmin) : IRequest;
public record BulkStageCommand(BulkStageRequest Request, Guid CurrentUserId, bool IsAdmin) : IRequest;
public record BulkDeleteCommand(BulkDeleteRequest Request, Guid CurrentUserId, bool IsAdmin) : IRequest;

// Handlers
public class BulkAssignHandler(IAppDbContext context) : IRequestHandler<BulkAssignCommand>
{
    public async Task Handle(BulkAssignCommand cmd, CancellationToken ct)
    {
        if (!cmd.IsAdmin)
            throw new UnauthorizedAccessException("Only administrators can perform bulk assignment.");

        var rep = await context.Users.FindAsync([cmd.Request.UserId], ct)
            ?? throw new KeyNotFoundException("Sales representative not found.");

        var companies = await context.Companies
            .Where(c => cmd.Request.CompanyIds.Contains(c.Id))
            .ToListAsync(ct);

        foreach (var company in companies)
        {
            company.AssignedToUserId = cmd.Request.UserId;
        }

        await context.SaveChangesAsync(ct);
    }
}

public class BulkStageHandler(IAppDbContext context) : IRequestHandler<BulkStageCommand>
{
    public async Task Handle(BulkStageCommand cmd, CancellationToken ct)
    {
        if (!Enum.TryParse<Stage>(cmd.Request.Stage, true, out var targetStage))
            throw new ArgumentException($"Invalid stage: {cmd.Request.Stage}");

        var companies = await context.Companies
            .Where(c => cmd.Request.CompanyIds.Contains(c.Id))
            .ToListAsync(ct);

        foreach (var company in companies)
        {
            // Authorization: Admin can edit any, Sales can edit only their assigned companies
            if (!cmd.IsAdmin && company.AssignedToUserId != cmd.CurrentUserId)
                throw new UnauthorizedAccessException($"Access denied for company {company.EnglishName}.");

            var oldStage = company.Stage;
            if (oldStage != targetStage)
            {
                company.Stage = targetStage;
                
                // Log Stage History
                context.StageHistories.Add(new StageHistory
                {
                    CompanyId = company.Id,
                    FromStage = oldStage.ToString(),
                    ToStage = targetStage.ToString(),
                    ChangedByUserId = cmd.CurrentUserId,
                    TenantId = company.TenantId,
                    Reason = "Stage transitioned via bulk update operation."
                });
            }
        }

        await context.SaveChangesAsync(ct);
    }
}

public class BulkDeleteHandler(IAppDbContext context) : IRequestHandler<BulkDeleteCommand>
{
    public async Task Handle(BulkDeleteCommand cmd, CancellationToken ct)
    {
        var companies = await context.Companies
            .Where(c => cmd.Request.CompanyIds.Contains(c.Id))
            .ToListAsync(ct);

        foreach (var company in companies)
        {
            // Authorization: Admin can edit any, Sales can edit only their assigned companies
            if (!cmd.IsAdmin && company.AssignedToUserId != cmd.CurrentUserId)
                throw new UnauthorizedAccessException($"Access denied for company {company.EnglishName}.");

            company.IsActive = false; // Soft delete
        }

        await context.SaveChangesAsync(ct);
    }
}
