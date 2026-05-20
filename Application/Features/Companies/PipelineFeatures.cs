using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Companies;

public record PipelineStageSummary(string Stage, int Count, decimal TotalExpectedRevenue);

public record PipelineSummaryDto(IEnumerable<PipelineStageSummary> Stages, decimal TotalPipelineValue);

public record GetPipelineSummaryQuery(Guid CurrentUserId, bool IsAdmin) : IRequest<PipelineSummaryDto>;

public class GetPipelineSummaryHandler(IAppDbContext context)
    : IRequestHandler<GetPipelineSummaryQuery, PipelineSummaryDto>
{
    public async Task<PipelineSummaryDto> Handle(GetPipelineSummaryQuery q, CancellationToken ct)
    {
        var query = context.Companies.Where(c => c.IsActive);

        if (!q.IsAdmin)
            query = query.Where(c => c.AssignedToUserId == q.CurrentUserId);

        var groupedData = await query
            .GroupBy(c => c.Stage)
            .Select(g => new
            {
                Stage = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(c => c.ExpectedRevenue ?? 0)
            })
            .ToListAsync(ct);

        var allStages = Enum.GetValues<Stage>();
        var stagesSummary = allStages.Select(s =>
        {
            var matched = groupedData.FirstOrDefault(g => g.Stage == s);
            return new PipelineStageSummary(
                s.ToString(),
                matched?.Count ?? 0,
                matched?.Revenue ?? 0);
        }).ToList();

        var totalPipelineValue = stagesSummary.Sum(s => s.TotalExpectedRevenue);

        return new PipelineSummaryDto(stagesSummary, totalPipelineValue);
    }
}
