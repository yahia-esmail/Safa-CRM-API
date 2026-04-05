using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Solutions;

// --- DTOs ---
public record SolutionDto(Guid Id, string Name, string? Description, bool IsActive, DateTime CreatedAt);
public record CreateSolutionRequest(string Name, string? Description);
public record UpdateSolutionRequest(string Name, string? Description, bool IsActive);

// --- Commands & Queries ---
public record GetSolutionsQuery(bool? ActiveOnly = true) : IRequest<IEnumerable<SolutionDto>>;
public record GetSolutionByIdQuery(Guid Id) : IRequest<SolutionDto>;
public record CreateSolutionCommand(CreateSolutionRequest Request) : IRequest<SolutionDto>;
public record UpdateSolutionCommand(Guid Id, UpdateSolutionRequest Request) : IRequest<SolutionDto>;

// --- Handlers ---
public class GetSolutionsHandler(IAppDbContext context) : IRequestHandler<GetSolutionsQuery, IEnumerable<SolutionDto>>
{
    public async Task<IEnumerable<SolutionDto>> Handle(GetSolutionsQuery q, CancellationToken ct)
    {
        var query = context.TechSolutions.AsQueryable();
        if (q.ActiveOnly == true) query = query.Where(s => s.IsActive);
        return await query
            .OrderBy(s => s.Name)
            .Select(s => new SolutionDto(s.Id, s.Name, s.Description, s.IsActive, s.CreatedAt))
            .ToListAsync(ct);
    }
}

public class CreateSolutionHandler(IAppDbContext context) : IRequestHandler<CreateSolutionCommand, SolutionDto>
{
    public async Task<SolutionDto> Handle(CreateSolutionCommand cmd, CancellationToken ct)
    {
        var solution = new TechSolution { Name = cmd.Request.Name, Description = cmd.Request.Description };
        context.TechSolutions.Add(solution);
        await context.SaveChangesAsync(ct);
        return new SolutionDto(solution.Id, solution.Name, solution.Description, solution.IsActive, solution.CreatedAt);
    }
}

public class UpdateSolutionHandler(IAppDbContext context) : IRequestHandler<UpdateSolutionCommand, SolutionDto>
{
    public async Task<SolutionDto> Handle(UpdateSolutionCommand cmd, CancellationToken ct)
    {
        var solution = await context.TechSolutions.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException("Solution not found.");
        solution.Name = cmd.Request.Name;
        solution.Description = cmd.Request.Description;
        solution.IsActive = cmd.Request.IsActive;
        await context.SaveChangesAsync(ct);
        return new SolutionDto(solution.Id, solution.Name, solution.Description, solution.IsActive, solution.CreatedAt);
    }
}
