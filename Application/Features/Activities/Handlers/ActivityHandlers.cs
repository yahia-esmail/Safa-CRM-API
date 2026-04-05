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
            Note = cmd.Request.Note
        };

        context.Activities.Add(activity);
        await context.SaveChangesAsync(ct);

        var user = await context.Users.FindAsync([cmd.CurrentUserId], ct);
        return new ActivityDto(
            activity.Id, activity.CompanyId, company.EnglishName,
            activity.Type.ToString(), activity.Note,
            activity.CreatedByUserId, user?.Name ?? "", activity.CreatedAt);
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
                a.CreatedByUserId, a.CreatedBy.Name, a.CreatedAt))
            .ToListAsync(ct);
    }
}
