using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Activities.Handlers;

public class CreateActivityHandler(IAppDbContext context)
    : IRequestHandler<CreateActivityCommand, ActivityDto>
{
    public async Task<ActivityDto> Handle(CreateActivityCommand cmd, CancellationToken ct)
    {
        var company = await context.Companies.FindAsync([cmd.Request.CompanyId], ct)
            ?? throw new KeyNotFoundException("Company not found.");

        if (!cmd.IsAdmin && company.AssignedToUserId != cmd.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        if (!Enum.TryParse<Domain.Enums.ActivityType>(cmd.Request.Type, true, out var actType))
            throw new ArgumentException($"Invalid activity type: {cmd.Request.Type}");

        var activity = new Activity
        {
            CompanyId = cmd.Request.CompanyId,
            CreatedByUserId = cmd.CurrentUserId,
            Type = actType,
            Note = cmd.Request.Note,
            DueDate = cmd.Request.DueDate,
            IsCompleted = false,
            CompletedAt = null
        };

        context.Activities.Add(activity);
        await context.SaveChangesAsync(ct);

        var user = await context.Users.FindAsync([cmd.CurrentUserId], ct);
        return new ActivityDto(
            activity.Id, activity.CompanyId, company.EnglishName,
            activity.Type.ToString(), activity.Note,
            activity.CreatedByUserId, user?.Name ?? "", activity.CreatedAt,
            activity.DueDate, activity.IsCompleted, activity.CompletedAt);
    }
}

public class UpdateActivityHandler(IAppDbContext context)
    : IRequestHandler<UpdateActivityCommand, ActivityDto>
{
    public async Task<ActivityDto> Handle(UpdateActivityCommand cmd, CancellationToken ct)
    {
        var activity = await context.Activities
            .Include(a => a.Company)
            .Include(a => a.CreatedBy)
            .FirstOrDefaultAsync(a => a.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException("Activity not found.");

        if (!cmd.IsAdmin && activity.Company.AssignedToUserId != cmd.CurrentUserId && activity.CreatedByUserId != cmd.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        if (!Enum.TryParse<Domain.Enums.ActivityType>(cmd.Request.Type, true, out var actType))
            throw new ArgumentException($"Invalid activity type: {cmd.Request.Type}");

        activity.Type = actType;
        activity.Note = cmd.Request.Note;
        activity.DueDate = cmd.Request.DueDate;
        
        if (activity.IsCompleted != cmd.Request.IsCompleted)
        {
            activity.IsCompleted = cmd.Request.IsCompleted;
            activity.CompletedAt = cmd.Request.IsCompleted ? DateTime.UtcNow : null;
        }

        await context.SaveChangesAsync(ct);

        return new ActivityDto(
            activity.Id, activity.CompanyId, activity.Company.EnglishName,
            activity.Type.ToString(), activity.Note,
            activity.CreatedByUserId, activity.CreatedBy.Name, activity.CreatedAt,
            activity.DueDate, activity.IsCompleted, activity.CompletedAt);
    }
}

public class ToggleActivityCompletionHandler(IAppDbContext context)
    : IRequestHandler<ToggleActivityCompletionCommand, ActivityDto>
{
    public async Task<ActivityDto> Handle(ToggleActivityCompletionCommand cmd, CancellationToken ct)
    {
        var activity = await context.Activities
            .Include(a => a.Company)
            .Include(a => a.CreatedBy)
            .FirstOrDefaultAsync(a => a.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException("Activity not found.");

        if (!cmd.IsAdmin && activity.Company.AssignedToUserId != cmd.CurrentUserId && activity.CreatedByUserId != cmd.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        activity.IsCompleted = cmd.IsCompleted;
        activity.CompletedAt = cmd.IsCompleted ? DateTime.UtcNow : null;

        await context.SaveChangesAsync(ct);

        return new ActivityDto(
            activity.Id, activity.CompanyId, activity.Company.EnglishName,
            activity.Type.ToString(), activity.Note,
            activity.CreatedByUserId, activity.CreatedBy.Name, activity.CreatedAt,
            activity.DueDate, activity.IsCompleted, activity.CompletedAt);
    }
}

public class DeleteActivityHandler(IAppDbContext context)
    : IRequestHandler<DeleteActivityCommand>
{
    public async Task Handle(DeleteActivityCommand cmd, CancellationToken ct)
    {
        var activity = await context.Activities
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException("Activity not found.");

        if (!cmd.IsAdmin && activity.Company.AssignedToUserId != cmd.CurrentUserId && activity.CreatedByUserId != cmd.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        context.Activities.Remove(activity);
        await context.SaveChangesAsync(ct);
    }
}

public class GetCompanyActivitiesHandler(IAppDbContext context)
    : IRequestHandler<GetCompanyActivitiesQuery, IEnumerable<ActivityDto>>
{
    public async Task<IEnumerable<ActivityDto>> Handle(GetCompanyActivitiesQuery q, CancellationToken ct)
    {
        var company = await context.Companies.FindAsync([q.CompanyId], ct)
            ?? throw new KeyNotFoundException("Company not found.");

        if (!q.IsAdmin && company.AssignedToUserId != q.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        return await context.Activities
            .Include(a => a.CreatedBy)
            .Include(a => a.Company)
            .Where(a => a.CompanyId == q.CompanyId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ActivityDto(
                a.Id, a.CompanyId, a.Company.EnglishName,
                a.Type.ToString(), a.Note,
                a.CreatedByUserId, a.CreatedBy.Name, a.CreatedAt,
                a.DueDate, a.IsCompleted, a.CompletedAt))
            .ToListAsync(ct);
    }
}

public class GetMyTasksHandler(IAppDbContext context)
    : IRequestHandler<GetMyTasksQuery, IEnumerable<ActivityDto>>
{
    public async Task<IEnumerable<ActivityDto>> Handle(GetMyTasksQuery q, CancellationToken ct)
    {
        var query = context.Activities
            .Include(a => a.CreatedBy)
            .Include(a => a.Company)
            .AsQueryable();

        if (!q.IsAdmin)
        {
            query = query.Where(a => a.CreatedByUserId == q.CurrentUserId || a.Company.AssignedToUserId == q.CurrentUserId);
        }

        if (q.IsCompleted.HasValue)
        {
            query = query.Where(a => a.IsCompleted == q.IsCompleted.Value);
        }

        return await query
            .OrderByDescending(a => a.DueDate)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new ActivityDto(
                a.Id, a.CompanyId, a.Company.EnglishName,
                a.Type.ToString(), a.Note,
                a.CreatedByUserId, a.CreatedBy.Name, a.CreatedAt,
                a.DueDate, a.IsCompleted, a.CompletedAt))
            .ToListAsync(ct);
    }
}
