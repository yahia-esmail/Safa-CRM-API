using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Dashboard;

// --- DTOs ---
public record SalesByCountryDto(string Country, decimal UsdAmount, int Count);
public record NewVsExistingDto(int Count, decimal UsdAmount);
public record NewVsExistingGroupDto(NewVsExistingDto NewCompanies, NewVsExistingDto ExistingCompanies);
public record SalesByRepDto(Guid RepId, string RepName, decimal UsdAmount, int CompaniesCount);
public record TopSolutionDto(string Solution, decimal UsdAmount, int Count);
public record PipelineStageDto(string Stage, int Count, decimal ExpectedRevenue);

public record AdminDashboardDto(
    decimal TotalSalesUsd,
    IEnumerable<SalesByCountryDto> SalesByCountry,
    NewVsExistingGroupDto NewVsExisting,
    IEnumerable<SalesByRepDto> SalesByRep,
    IEnumerable<TopSolutionDto> TopSolutions,
    IEnumerable<PipelineStageDto> PipelineByStage);

public record SalesDashboardDto(
    decimal TotalSalesUsd,
    int TotalCompanies,
    int TotalOrders,
    IEnumerable<PipelineStageDto> PipelineByStage,
    IEnumerable<TopSolutionDto> TopSolutions);

// --- Queries ---
public record GetAdminDashboardQuery(DateTime? From, DateTime? To) : IRequest<AdminDashboardDto>;
public record GetSalesDashboardQuery(Guid UserId, DateTime? From, DateTime? To) : IRequest<SalesDashboardDto>;

// --- Handlers ---
public class GetAdminDashboardHandler(IAppDbContext context)
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery q, CancellationToken ct)
    {
        var from = q.From ?? DateTime.UtcNow.AddMonths(-12);
        var to = q.To ?? DateTime.UtcNow;

        var confirmedOrders = context.SalesOrders
            .Include(o => o.Company)
            .Include(o => o.CreatedBy)
            .Include(o => o.Items).ThenInclude(i => i.Solution)
            .Where(o => o.Status == OrderStatus.Confirmed
                     && o.CreatedAt >= from && o.CreatedAt <= to);

        var totalSales = await confirmedOrders.SumAsync(o => o.UsdAmount, ct);

        var byCountry = await confirmedOrders
            .GroupBy(o => o.Company.Country)
            .Select(g => new SalesByCountryDto(g.Key, g.Sum(o => o.UsdAmount), g.Count()))
            .OrderByDescending(x => x.UsdAmount)
            .ToListAsync(ct);

        // New vs Existing
        var allFirstOrders = await context.SalesOrders
            .Where(o => o.Status == OrderStatus.Confirmed)
            .GroupBy(o => o.CompanyId)
            .Select(g => new { CompanyId = g.Key, First = g.Min(o => o.CreatedAt) })
            .ToListAsync(ct);

        var newIds = allFirstOrders
            .Where(x => x.First >= from && x.First <= to)
            .Select(x => x.CompanyId).ToHashSet();

        var newGrp = await confirmedOrders.Where(o => newIds.Contains(o.CompanyId))
            .GroupBy(_ => 1).Select(g => new { Count = g.Select(o => o.CompanyId).Distinct().Count(), Usd = g.Sum(o => o.UsdAmount) })
            .FirstOrDefaultAsync(ct);
        var existGrp = await confirmedOrders.Where(o => !newIds.Contains(o.CompanyId))
            .GroupBy(_ => 1).Select(g => new { Count = g.Select(o => o.CompanyId).Distinct().Count(), Usd = g.Sum(o => o.UsdAmount) })
            .FirstOrDefaultAsync(ct);

        var byRep = await confirmedOrders
            .GroupBy(o => new { o.CreatedByUserId, o.CreatedBy.Name })
            .Select(g => new SalesByRepDto(g.Key.CreatedByUserId, g.Key.Name, g.Sum(o => o.UsdAmount),
                g.Select(o => o.CompanyId).Distinct().Count()))
            .OrderByDescending(x => x.UsdAmount)
            .ToListAsync(ct);

        var topSolutions = await context.SalesOrderItems
            .Include(i => i.SalesOrder).Include(i => i.Solution)
            .Where(i => i.SalesOrder.Status == OrderStatus.Confirmed
                     && i.SalesOrder.CreatedAt >= from && i.SalesOrder.CreatedAt <= to)
            .GroupBy(i => i.Solution.Name)
            .Select(g => new TopSolutionDto(g.Key, g.Sum(i => i.Price), g.Count()))
            .OrderByDescending(x => x.UsdAmount).Take(10)
            .ToListAsync(ct);

        var pipeline = await context.Companies
            .Where(c => c.IsActive)
            .GroupBy(c => c.Stage)
            .Select(g => new PipelineStageDto(g.Key.ToString(), g.Count(), g.Sum(c => c.ExpectedRevenue ?? 0)))
            .ToListAsync(ct);

        return new AdminDashboardDto(totalSales, byCountry,
            new NewVsExistingGroupDto(
                new NewVsExistingDto(newGrp?.Count ?? 0, newGrp?.Usd ?? 0),
                new NewVsExistingDto(existGrp?.Count ?? 0, existGrp?.Usd ?? 0)),
            byRep, topSolutions, pipeline);
    }
}

public class GetSalesDashboardHandler(IAppDbContext context)
    : IRequestHandler<GetSalesDashboardQuery, SalesDashboardDto>
{
    public async Task<SalesDashboardDto> Handle(GetSalesDashboardQuery q, CancellationToken ct)
    {
        var from = q.From ?? DateTime.UtcNow.AddMonths(-12);
        var to = q.To ?? DateTime.UtcNow;

        var myOrders = context.SalesOrders
            .Include(o => o.Items).ThenInclude(i => i.Solution)
            .Where(o => o.CreatedByUserId == q.UserId
                     && o.Status == OrderStatus.Confirmed
                     && o.CreatedAt >= from && o.CreatedAt <= to);

        var totalSales = await myOrders.SumAsync(o => o.UsdAmount, ct);
        var totalOrders = await myOrders.CountAsync(ct);
        var myCompanies = await context.Companies
            .CountAsync(c => c.AssignedToUserId == q.UserId && c.IsActive, ct);

        var pipeline = await context.Companies
            .Where(c => c.IsActive && c.AssignedToUserId == q.UserId)
            .GroupBy(c => c.Stage)
            .Select(g => new PipelineStageDto(g.Key.ToString(), g.Count(), g.Sum(c => c.ExpectedRevenue ?? 0)))
            .ToListAsync(ct);

        var topSolutions = await context.SalesOrderItems
            .Include(i => i.SalesOrder).Include(i => i.Solution)
            .Where(i => i.SalesOrder.CreatedByUserId == q.UserId
                     && i.SalesOrder.Status == OrderStatus.Confirmed
                     && i.SalesOrder.CreatedAt >= from && i.SalesOrder.CreatedAt <= to)
            .GroupBy(i => i.Solution.Name)
            .Select(g => new TopSolutionDto(g.Key, g.Sum(i => i.Price), g.Count()))
            .OrderByDescending(x => x.UsdAmount).Take(5)
            .ToListAsync(ct);

        return new SalesDashboardDto(totalSales, myCompanies, totalOrders, pipeline, topSolutions);
    }
}
