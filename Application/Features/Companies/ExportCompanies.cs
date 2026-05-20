using Application.Common.Interfaces;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Companies;

public record GetCompaniesExportQuery(
    string? Name,
    string? Country,
    int? SafaKey,
    string? Email,
    string? Phone,
    string? AccountType,
    string? Stage,
    string? LeadStatus,
    Guid? AssignedTo,
    Guid? TagId,
    Guid CurrentUserId,
    bool IsAdmin) : IRequest<byte[]>;

public class GetCompaniesExportHandler(ICompanyRepository repo, IAppDbContext context, IExportService exportService)
    : IRequestHandler<GetCompaniesExportQuery, byte[]>
{
    public async Task<byte[]> Handle(GetCompaniesExportQuery q, CancellationToken ct)
    {
        var (items, _) = await repo.SearchAsync(
            q.Name, q.Country, q.SafaKey, q.Email, q.Phone,
            q.AccountType, q.Stage, q.LeadStatus, q.AssignedTo,
            q.CurrentUserId, q.IsAdmin, 1, int.MaxValue, q.TagId);

        var companyIds = items.Select(c => c.Id).ToList();
        
        var lastActivities = await context.Activities
            .Where(a => companyIds.Contains(a.CompanyId))
            .GroupBy(a => a.CompanyId)
            .Select(g => new { CompanyId = g.Key, MaxDate = g.Max(a => a.CreatedAt) })
            .ToDictionaryAsync(x => x.CompanyId, x => x.MaxDate, ct);

        var exportRows = items.Select(c => new CompanyExportRow(
            c.EnglishName,
            c.ArabicName,
            c.Country,
            c.Phone,
            c.Email,
            c.Stage.ToString(),
            c.AccountType,
            c.AssignedTo?.Name,
            c.CreatedAt,
            lastActivities.TryGetValue(c.Id, out var maxDate) ? maxDate : null
        ));

        return exportService.ExportCompanies(exportRows);
    }
}
