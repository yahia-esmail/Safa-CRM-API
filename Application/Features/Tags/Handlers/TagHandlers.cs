using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Tags.Handlers;

public class GetTagsHandler(IAppDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetTagsQuery, IEnumerable<TagDto>>
{
    public async Task<IEnumerable<TagDto>> Handle(GetTagsQuery q, CancellationToken ct)
    {
        var tenantId = currentUserService.TenantId;

        var tags = await context.CompanyTags
            .Include(t => t.CreatedBy)
            .Where(t => t.CreatedBy.TenantId == tenantId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return tags.Select(t => new TagDto(
            t.Id,
            t.Name,
            t.Color,
            t.CreatedByUserId,
            t.CreatedBy.Name));
    }
}

public class CreateTagHandler(IAppDbContext context)
    : IRequestHandler<CreateTagCommand, TagDto>
{
    public async Task<TagDto> Handle(CreateTagCommand cmd, CancellationToken ct)
    {
        var user = await context.Users.FindAsync([cmd.CreatedByUserId], ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        // Check duplicate name within the same tenant
        var exists = await context.CompanyTags
            .AnyAsync(t => t.Name.ToLower() == cmd.Request.Name.ToLower() && t.CreatedBy.TenantId == user.TenantId, ct);

        if (exists)
            throw new ArgumentException("A tag with this name already exists.");

        var tag = new CompanyTag
            {
            Name = cmd.Request.Name,
            Color = cmd.Request.Color,
            CreatedByUserId = cmd.CreatedByUserId
        };

        context.CompanyTags.Add(tag);
        await context.SaveChangesAsync(ct);

        return new TagDto(tag.Id, tag.Name, tag.Color, tag.CreatedByUserId, user.Name);
    }
}

public class DeleteTagHandler(IAppDbContext context)
    : IRequestHandler<DeleteTagCommand>
{
    public async Task Handle(DeleteTagCommand cmd, CancellationToken ct)
    {
        if (!cmd.IsAdmin)
            throw new UnauthorizedAccessException("Only administrators can delete tags.");

        var tag = await context.CompanyTags.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException("Tag not found.");

        context.CompanyTags.Remove(tag);
        await context.SaveChangesAsync(ct);
    }
}

public class AssignTagHandler(IAppDbContext context)
    : IRequestHandler<AssignTagCommand>
{
    public async Task Handle(AssignTagCommand cmd, CancellationToken ct)
    {
        var company = await context.Companies.FindAsync([cmd.CompanyId], ct)
            ?? throw new KeyNotFoundException("Company not found.");

        if (!cmd.IsAdmin && company.AssignedToUserId != cmd.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        var tag = await context.CompanyTags.FindAsync([cmd.TagId], ct)
            ?? throw new KeyNotFoundException("Tag not found.");

        var alreadyAssigned = await context.CompanyTagAssignments
            .AnyAsync(a => a.CompanyId == cmd.CompanyId && a.TagId == cmd.TagId, ct);

        if (!alreadyAssigned)
        {
            var assignment = new CompanyTagAssignment
            {
                CompanyId = cmd.CompanyId,
                TagId = cmd.TagId
            };
            context.CompanyTagAssignments.Add(assignment);
            await context.SaveChangesAsync(ct);
        }
    }
}

public class RemoveTagHandler(IAppDbContext context)
    : IRequestHandler<RemoveTagCommand>
{
    public async Task Handle(RemoveTagCommand cmd, CancellationToken ct)
    {
        var company = await context.Companies.FindAsync([cmd.CompanyId], ct)
            ?? throw new KeyNotFoundException("Company not found.");

        if (!cmd.IsAdmin && company.AssignedToUserId != cmd.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        var assignment = await context.CompanyTagAssignments
            .FirstOrDefaultAsync(a => a.CompanyId == cmd.CompanyId && a.TagId == cmd.TagId, ct);

        if (assignment != null)
        {
            context.CompanyTagAssignments.Remove(assignment);
            await context.SaveChangesAsync(ct);
        }
    }
}
