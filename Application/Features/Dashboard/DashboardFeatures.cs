using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Dashboard;

// ── Shared DTOs ───────────────────────────────────────────────────────────────
public record SalesByCountryDto(string Country, decimal UsdAmount, int OrderCount);
public record NewVsExistingDto(int Count, decimal UsdAmount);
public record NewVsExistingGroupDto(NewVsExistingDto NewCompanies, NewVsExistingDto ExistingCompanies);
public record SalesByRepDto(Guid RepId, string RepName, decimal UsdAmount, int OrderCount, int CompaniesCount);
public record TopSolutionDto(string Solution, decimal UsdAmount, int Count);
public record PipelineStageDto(string Stage, int Count, decimal ExpectedRevenue);
public record OrdersByStatusDto(int Draft, int Confirmed, int Cancelled);
public record RecentOrderDto(
    Guid Id, string InvoiceNumber, string CompanyName,
    string Status, decimal OriginalAmount, string Currency, DateTime CreatedAt);
public record RecentActivityDto(
    Guid Id, string CompanyName, string ActivityType,
    string Note, string CreatedBy, DateTime CreatedAt);

public record DashboardRecentActivityDto(string Type, string Company, string By, DateTime At);
public record UpcomingRenewalDto(string Company, DateOnly EndDate, int DaysLeft, decimal UsdAmount);
public record OverdueTaskDto(string Company, string TaskNote, DateTime DueDate);
public record NoActivityCompanyDto(Guid Id, string Name, int DaysSinceActivity);

// ── Admin DTO ─────────────────────────────────────────────────────────────────
public record AdminDashboardDto(
    // KPI Cards
    int     TotalCompanies,
    int     TotalActiveCompanies,
    int     TotalOrders,
    decimal TotalSalesUsd,
    int     NewCompaniesThisMonth,
    int     NewOrdersThisMonth,
    // Orders breakdown
    OrdersByStatusDto          OrdersByStatus,
    // Charts
    IEnumerable<SalesByCountryDto>  SalesByCountry,
    NewVsExistingGroupDto           NewVsExisting,
    IEnumerable<SalesByRepDto>      SalesByRep,
    IEnumerable<TopSolutionDto>     TopSolutions,
    IEnumerable<PipelineStageDto>   PipelineByStage,
    // Recent activity feed
    IEnumerable<RecentOrderDto>     RecentOrders,
    IEnumerable<DashboardRecentActivityDto> RecentActivities,
    IEnumerable<UpcomingRenewalDto> UpcomingRenewals,
    IEnumerable<OverdueTaskDto>     OverdueTasks,
    IEnumerable<NoActivityCompanyDto> CompaniesWithNoActivity);

// ── Sales DTO ─────────────────────────────────────────────────────────────────
public record SalesDashboardDto(
    // KPI Cards
    decimal TotalSalesUsd,
    int     TotalCompanies,
    int     TotalOrders,
    int     NewCompaniesThisMonth,
    // Charts
    IEnumerable<PipelineStageDto>  PipelineByStage,
    IEnumerable<TopSolutionDto>    TopSolutions,
    // Recent feed
    IEnumerable<RecentOrderDto>       RecentOrders,
    IEnumerable<DashboardRecentActivityDto> RecentActivities,
    IEnumerable<UpcomingRenewalDto> UpcomingRenewals,
    IEnumerable<OverdueTaskDto>     OverdueTasks,
    IEnumerable<NoActivityCompanyDto> CompaniesWithNoActivity);

// ── Queries ───────────────────────────────────────────────────────────────────
public record GetAdminDashboardQuery(DateTime? From, DateTime? To)  : IRequest<AdminDashboardDto>;
public record GetSalesDashboardQuery(Guid UserId, DateTime? From, DateTime? To) : IRequest<SalesDashboardDto>;

// ═════════════════════════════════════════════════════════════════════════════
// Admin Handler
// ═════════════════════════════════════════════════════════════════════════════
public class GetAdminDashboardHandler(IAppDbContext context)
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery q, CancellationToken ct)
    {
        var from = q.From ?? DateTime.UtcNow.AddMonths(-12);
        var to   = q.To   ?? DateTime.UtcNow;
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        // ── KPI counts ────────────────────────────────────────────────────
        var totalCompanies       = await context.Companies.CountAsync(ct);
        var totalActiveCompanies = await context.Companies.CountAsync(c => c.IsActive, ct);
        var newCompaniesMonth    = await context.Companies.CountAsync(c => c.CreatedAt >= monthStart, ct);

        var allOrdersInPeriod = await context.SalesOrders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .Select(o => new { o.CompanyId, o.UsdAmount, o.Status,
                               Country = o.Company.Country,
                               RepId   = o.CreatedByUserId,
                               RepName = o.CreatedBy.Name })
            .ToListAsync(ct);

        var totalOrders     = allOrdersInPeriod.Count;
        var newOrdersMonth  = await context.SalesOrders.CountAsync(o => o.CreatedAt >= monthStart, ct);

        var ordersByStatus = new OrdersByStatusDto(
            allOrdersInPeriod.Count(o => o.Status == OrderStatus.Draft),
            allOrdersInPeriod.Count(o => o.Status == OrderStatus.Confirmed),
            allOrdersInPeriod.Count(o => o.Status == OrderStatus.Cancelled));

        var confirmedOrders = allOrdersInPeriod.Where(o => o.Status == OrderStatus.Confirmed).ToList();
        var totalSales      = confirmedOrders.Sum(o => o.UsdAmount);

        // ── Sales by country ──────────────────────────────────────────────
        var byCountry = confirmedOrders
            .GroupBy(o => o.Country)
            .Select(g => new SalesByCountryDto(g.Key, g.Sum(o => o.UsdAmount), g.Count()))
            .OrderByDescending(x => x.UsdAmount)
            .ToList();

        // ── New vs Existing ───────────────────────────────────────────────
        var allFirstOrders = await context.SalesOrders
            .Where(o => o.Status == OrderStatus.Confirmed)
            .GroupBy(o => o.CompanyId)
            .Select(g => new { CompanyId = g.Key, First = g.Min(o => o.CreatedAt) })
            .ToListAsync(ct);

        var newIds      = allFirstOrders.Where(x => x.First >= from && x.First <= to).Select(x => x.CompanyId).ToHashSet();
        var newData     = confirmedOrders.Where(o =>  newIds.Contains(o.CompanyId)).ToList();
        var existData   = confirmedOrders.Where(o => !newIds.Contains(o.CompanyId)).ToList();

        var newVsExisting = new NewVsExistingGroupDto(
            new NewVsExistingDto(newData.Select(o => o.CompanyId).Distinct().Count(), newData.Sum(o => o.UsdAmount)),
            new NewVsExistingDto(existData.Select(o => o.CompanyId).Distinct().Count(), existData.Sum(o => o.UsdAmount)));

        // ── Sales by rep ──────────────────────────────────────────────────
        var byRep = confirmedOrders
            .GroupBy(o => new { o.RepId, o.RepName })
            .Select(g => new SalesByRepDto(
                g.Key.RepId, g.Key.RepName,
                g.Sum(o => o.UsdAmount),
                g.Count(),
                g.Select(o => o.CompanyId).Distinct().Count()))
            .OrderByDescending(x => x.UsdAmount)
            .ToList();

        // ── Top solutions ─────────────────────────────────────────────────
        var solutionsRaw = await context.SalesOrderItems
            .Where(i => i.SalesOrder.Status == OrderStatus.Confirmed
                     && i.SalesOrder.CreatedAt >= from && i.SalesOrder.CreatedAt <= to)
            .Select(i => new { i.Solution.Name, i.Price })
            .ToListAsync(ct);

        var topSolutions = solutionsRaw
            .GroupBy(i => i.Name)
            .Select(g => new TopSolutionDto(g.Key, g.Sum(i => i.Price), g.Count()))
            .OrderByDescending(x => x.UsdAmount).Take(10)
            .ToList();

        // ── Pipeline by stage ─────────────────────────────────────────────
        var pipelineRaw = await context.Companies
            .Where(c => c.IsActive)
            .Select(c => new { c.Stage, c.ExpectedRevenue })
            .ToListAsync(ct);

        var pipeline = pipelineRaw
            .GroupBy(c => c.Stage)
            .Select(g => new PipelineStageDto(g.Key.ToString(), g.Count(), g.Sum(c => c.ExpectedRevenue ?? 0)))
            .OrderBy(x => x.Stage)
            .ToList();

        // ── Recent orders (last 10) ───────────────────────────────────────
        var recentOrders = await context.SalesOrders
            .Include(o => o.Company)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new RecentOrderDto(
                o.Id, o.InvoiceNumber, o.Company.EnglishName,
                o.Status.ToString(), o.OriginalAmount,
                o.OriginalCurrency.ToString(), o.CreatedAt))
            .ToListAsync(ct);

        // ── Recent activities (last 10) ───────────────────────────────────
        var recentActivities = await context.Activities
            .Include(a => a.Company)
            .Include(a => a.CreatedBy)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new DashboardRecentActivityDto(
                a.Type.ToString(), a.Company.EnglishName, a.CreatedBy.Name, a.CreatedAt))
            .ToListAsync(ct);

        // ── Upcoming renewals (next 30 days) ──────────────────────────────
        var todayOnly = DateOnly.FromDateTime(DateTime.UtcNow);
        var renewalLimit = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        var renewalsRaw = await context.SalesOrderItems
            .Include(i => i.SalesOrder).ThenInclude(o => o.Company)
            .Where(i => i.SalesOrder.Status == OrderStatus.Confirmed && i.EndDate.HasValue && i.EndDate >= todayOnly && i.EndDate <= renewalLimit)
            .OrderBy(i => i.EndDate)
            .Select(i => new {
                CompanyName = i.SalesOrder.Company.EnglishName,
                EndDate = i.EndDate!.Value,
                Price = i.Price,
                UsdRate = i.SalesOrder.UsdRateAtTime
            })
            .Take(10)
            .ToListAsync(ct);

        var upcomingRenewals = renewalsRaw.Select(r => new UpcomingRenewalDto(
            r.CompanyName,
            r.EndDate,
            (r.EndDate.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days,
            r.Price * r.UsdRate
        )).ToList();

        // ── Overdue tasks ─────────────────────────────────────────────────
        var overdueTasks = await context.Activities
            .Include(a => a.Company)
            .Where(a => a.Type == ActivityType.Task && !a.IsCompleted && a.DueDate < DateTime.UtcNow)
            .OrderBy(a => a.DueDate)
            .Select(a => new OverdueTaskDto(
                a.Company.EnglishName,
                a.Note,
                a.DueDate!.Value))
            .Take(10)
            .ToListAsync(ct);

        // ── Inactive companies (no activity for 21+ days) ──────────────────
        var inactiveLimit = DateTime.UtcNow.AddDays(-21);
        var noActivitiesRaw = await context.Companies
            .Where(c => c.IsActive && (!c.Activities.Any(a => a.CreatedAt >= inactiveLimit) && c.CreatedAt < inactiveLimit))
            .OrderBy(c => c.CreatedAt)
            .Select(c => new {
                c.Id,
                c.EnglishName,
                LastActivity = c.Activities.Any() ? c.Activities.Max(a => (DateTime?)a.CreatedAt) : (DateTime?)c.CreatedAt
            })
            .Take(10)
            .ToListAsync(ct);

        var companiesWithNoActivity = noActivitiesRaw.Select(c => new NoActivityCompanyDto(
            c.Id,
            c.EnglishName,
            c.LastActivity.HasValue ? (int)(DateTime.UtcNow - c.LastActivity.Value).TotalDays : 21
        )).ToList();

        return new AdminDashboardDto(
            totalCompanies, totalActiveCompanies, totalOrders,
            totalSales, newCompaniesMonth, newOrdersMonth,
            ordersByStatus, byCountry, newVsExisting,
            byRep, topSolutions, pipeline, recentOrders,
            recentActivities, upcomingRenewals, overdueTasks, companiesWithNoActivity);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// Sales Handler
// ═════════════════════════════════════════════════════════════════════════════
public class GetSalesDashboardHandler(IAppDbContext context)
    : IRequestHandler<GetSalesDashboardQuery, SalesDashboardDto>
{
    public async Task<SalesDashboardDto> Handle(GetSalesDashboardQuery q, CancellationToken ct)
    {
        var from       = q.From ?? DateTime.UtcNow.AddMonths(-12);
        var to         = q.To   ?? DateTime.UtcNow;
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        // ── KPI Cards ─────────────────────────────────────────────────────
        var myConfirmedOrders = await context.SalesOrders
            .Where(o => o.CreatedByUserId == q.UserId
                     && o.Status == OrderStatus.Confirmed
                     && o.CreatedAt >= from && o.CreatedAt <= to)
            .Select(o => o.UsdAmount)
            .ToListAsync(ct);

        var totalSales    = myConfirmedOrders.Sum();
        var totalOrders   = myConfirmedOrders.Count;
        var myCompanies   = await context.Companies.CountAsync(c => c.AssignedToUserId == q.UserId && c.IsActive, ct);
        var newCompanies  = await context.Companies.CountAsync(c => c.AssignedToUserId == q.UserId && c.CreatedAt >= monthStart, ct);

        // ── Pipeline ──────────────────────────────────────────────────────
        var pipelineRaw = await context.Companies
            .Where(c => c.IsActive && c.AssignedToUserId == q.UserId)
            .Select(c => new { c.Stage, c.ExpectedRevenue })
            .ToListAsync(ct);

        var pipeline = pipelineRaw
            .GroupBy(c => c.Stage)
            .Select(g => new PipelineStageDto(g.Key.ToString(), g.Count(), g.Sum(c => c.ExpectedRevenue ?? 0)))
            .OrderBy(x => x.Stage)
            .ToList();

        // ── Top solutions ─────────────────────────────────────────────────
        var solutionsRaw = await context.SalesOrderItems
            .Where(i => i.SalesOrder.CreatedByUserId == q.UserId
                     && i.SalesOrder.Status == OrderStatus.Confirmed
                     && i.SalesOrder.CreatedAt >= from && i.SalesOrder.CreatedAt <= to)
            .Select(i => new { i.Solution.Name, i.Price })
            .ToListAsync(ct);

        var topSolutions = solutionsRaw
            .GroupBy(i => i.Name)
            .Select(g => new TopSolutionDto(g.Key, g.Sum(i => i.Price), g.Count()))
            .OrderByDescending(x => x.UsdAmount).Take(5)
            .ToList();

        // ── Recent orders (my last 10) ────────────────────────────────────
        var recentOrders = await context.SalesOrders
            .Include(o => o.Company)
            .Where(o => o.CreatedByUserId == q.UserId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new RecentOrderDto(
                o.Id, o.InvoiceNumber, o.Company.EnglishName,
                o.Status.ToString(), o.OriginalAmount,
                o.OriginalCurrency.ToString(), o.CreatedAt))
            .ToListAsync(ct);

        // ── Recent activities (my companies, last 10) ─────────────────────
        var recentActivities = await context.Activities
            .Include(a => a.Company)
            .Include(a => a.CreatedBy)
            .Where(a => a.Company.AssignedToUserId == q.UserId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new DashboardRecentActivityDto(
                a.Type.ToString(), a.Company.EnglishName, a.CreatedBy.Name, a.CreatedAt))
            .ToListAsync(ct);

        // ── Upcoming renewals (my companies, next 30 days) ────────────────
        var todayOnly = DateOnly.FromDateTime(DateTime.UtcNow);
        var renewalLimit = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        var renewalsRaw = await context.SalesOrderItems
            .Include(i => i.SalesOrder).ThenInclude(o => o.Company)
            .Where(i => i.SalesOrder.Status == OrderStatus.Confirmed && 
                        i.SalesOrder.Company.AssignedToUserId == q.UserId &&
                        i.EndDate.HasValue && i.EndDate >= todayOnly && i.EndDate <= renewalLimit)
            .OrderBy(i => i.EndDate)
            .Select(i => new {
                CompanyName = i.SalesOrder.Company.EnglishName,
                EndDate = i.EndDate!.Value,
                Price = i.Price,
                UsdRate = i.SalesOrder.UsdRateAtTime
            })
            .Take(10)
            .ToListAsync(ct);

        var upcomingRenewals = renewalsRaw.Select(r => new UpcomingRenewalDto(
            r.CompanyName,
            r.EndDate,
            (r.EndDate.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days,
            r.Price * r.UsdRate
        )).ToList();

        // ── Overdue tasks (my companies) ──────────────────────────────────
        var overdueTasks = await context.Activities
            .Include(a => a.Company)
            .Where(a => a.Company.AssignedToUserId == q.UserId && a.Type == ActivityType.Task && !a.IsCompleted && a.DueDate < DateTime.UtcNow)
            .OrderBy(a => a.DueDate)
            .Select(a => new OverdueTaskDto(
                a.Company.EnglishName,
                a.Note,
                a.DueDate!.Value))
            .Take(10)
            .ToListAsync(ct);

        // ── Inactive companies (my companies, no activity for 21+ days) ───
        var inactiveLimit = DateTime.UtcNow.AddDays(-21);
        var noActivitiesRaw = await context.Companies
            .Where(c => c.AssignedToUserId == q.UserId && c.IsActive && (!c.Activities.Any(a => a.CreatedAt >= inactiveLimit) && c.CreatedAt < inactiveLimit))
            .OrderBy(c => c.CreatedAt)
            .Select(c => new {
                c.Id,
                c.EnglishName,
                LastActivity = c.Activities.Any() ? c.Activities.Max(a => (DateTime?)a.CreatedAt) : (DateTime?)c.CreatedAt
            })
            .Take(10)
            .ToListAsync(ct);

        var companiesWithNoActivity = noActivitiesRaw.Select(c => new NoActivityCompanyDto(
            c.Id,
            c.EnglishName,
            c.LastActivity.HasValue ? (int)(DateTime.UtcNow - c.LastActivity.Value).TotalDays : 21
        )).ToList();

        return new SalesDashboardDto(
            totalSales, myCompanies, totalOrders, newCompanies,
            pipeline, topSolutions, recentOrders, recentActivities,
            upcomingRenewals, overdueTasks, companiesWithNoActivity);
    }
}
