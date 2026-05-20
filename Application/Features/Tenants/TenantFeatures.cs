using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Tenants;

// ─── DTOs ────────────────────────────────────────────────────────────────────
public record PlanDto(Guid Id, string Name, int MaxAdmins, int MaxSales, decimal Price, int TenantCount);
public record TenantDto(Guid Id, string Name, string Industry, Guid SubscriptionPlanId, string PlanName, bool IsActive, DateTime SubscriptionStart, DateTime SubscriptionEnd, int UserCount);
public record TenantDetailDto(Guid Id, string Name, string Industry, Guid SubscriptionPlanId, string PlanName, bool IsActive, DateTime SubscriptionStart, DateTime SubscriptionEnd, int AdminCount, int SalesCount, int CompanyCount, int OrderCount);
public record SuperAdminDashboardDto(int TotalTenants, int ActiveTenants, int InactiveTenants, int TotalUsers, int TotalAdmins, int TotalSales, int TotalPlans, decimal TotalRevenue, IEnumerable<TenantSummaryDto> RecentTenants, IEnumerable<PlanUsageDto> PlanUsage);
public record TenantSummaryDto(Guid Id, string Name, string PlanName, bool IsActive, DateTime CreatedAt);
public record PlanUsageDto(string PlanName, int TenantCount, decimal Revenue);
public record AuditLogDto(Guid Id, string EntityName, string EntityId, string Action, string? OldValues, string? NewValues, string? UserName, string? IpAddress, DateTime CreatedAt);
public record UserProfileDto(Guid Id, string Name, string Email, string Role, bool IsActive, Guid? TenantId, string? TenantName, string? AvatarUrl, string? Phone);

// ─── Commands & Queries ───────────────────────────────────────────────────────
public record CreatePlanCommand(string Name, int MaxAdmins, int MaxSales, decimal Price) : IRequest<PlanDto>;
public record UpdatePlanCommand(Guid Id, string Name, int MaxAdmins, int MaxSales, decimal Price) : IRequest<PlanDto>;
public record DeletePlanCommand(Guid Id) : IRequest<bool>;
public record CreateTenantCommand(string Name, string Industry, Guid SubscriptionPlanId, int MonthsDuration) : IRequest<TenantDto>;
public record UpdateTenantCommand(Guid Id, string Name, string Industry, Guid SubscriptionPlanId, DateTime SubscriptionEnd) : IRequest<TenantDto>;
public record ToggleTenantStatusCommand(Guid TenantId, bool IsActive) : IRequest<bool>;
public record GetTenantsQuery : IRequest<IEnumerable<TenantDto>>;
public record GetTenantByIdQuery(Guid Id) : IRequest<TenantDetailDto>;
public record GetPlansQuery : IRequest<IEnumerable<PlanDto>>;
public record GetSuperAdminDashboardQuery : IRequest<SuperAdminDashboardDto>;
public record GetAuditLogsQuery(string? EntityName, string? Action, int Page, int PageSize) : IRequest<IEnumerable<AuditLogDto>>;
public record GetMyProfileQuery : IRequest<UserProfileDto>;
public record UpdateMyProfileCommand(string Name, string Email, string? Phone, string? AvatarUrl) : IRequest<UserProfileDto>;
public record ChangeMyPasswordCommand(string CurrentPassword, string NewPassword, string ConfirmPassword) : IRequest<bool>;

// --- Handlers ---
public class CreatePlanHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<CreatePlanCommand, PlanDto>
{
    public async Task<PlanDto> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != "SuperAdmin") throw new UnauthorizedAccessException("Only SuperAdmin can manage plans.");

        var plan = new SubscriptionPlan
        {
            Name = request.Name,
            MaxAdmins = request.MaxAdmins,
            MaxSales = request.MaxSales,
            Price = request.Price
        };

        context.SubscriptionPlans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);

        return new PlanDto(plan.Id, plan.Name, plan.MaxAdmins, plan.MaxSales, plan.Price, 0);
    }
}

public class CreateTenantHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<CreateTenantCommand, TenantDto>
{
    public async Task<TenantDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != "SuperAdmin") throw new UnauthorizedAccessException("Only SuperAdmin can manage tenants.");

        var plan = await context.SubscriptionPlans.FindAsync([request.SubscriptionPlanId], cancellationToken)
            ?? throw new KeyNotFoundException("Plan not found.");

        var tenant = new Tenant
        {
            Name = request.Name,
            Industry = request.Industry,
            SubscriptionPlanId = plan.Id,
            SubscriptionStart = DateTime.UtcNow,
            SubscriptionEnd = DateTime.UtcNow.AddMonths(request.MonthsDuration),
            IsActive = true
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(cancellationToken);

        return new TenantDto(tenant.Id, tenant.Name, tenant.Industry, tenant.SubscriptionPlanId, plan.Name, tenant.IsActive, tenant.SubscriptionStart, tenant.SubscriptionEnd, 0);
    }
}

public class ToggleTenantStatusHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<ToggleTenantStatusCommand, bool>
{
    public async Task<bool> Handle(ToggleTenantStatusCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != "SuperAdmin") throw new UnauthorizedAccessException("Only SuperAdmin can manage tenants.");

        var tenant = await context.Tenants
            .Include(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Tenant not found.");

        tenant.IsActive = request.IsActive;

        // Suspend or activate all users under this tenant
        foreach (var user in tenant.Users)
        {
            user.IsActive = request.IsActive;
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class GetTenantsHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<GetTenantsQuery, IEnumerable<TenantDto>>
{
    public async Task<IEnumerable<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != "SuperAdmin") throw new UnauthorizedAccessException("Only SuperAdmin can view tenants.");

        return await context.Tenants
            .Include(t => t.SubscriptionPlan)
            .Include(t => t.Users)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => task.Result.Select(t => new TenantDto(
                t.Id, t.Name, t.Industry, t.SubscriptionPlanId,
                t.SubscriptionPlan!.Name, t.IsActive,
                t.SubscriptionStart, t.SubscriptionEnd,
                t.Users.Count)));
    }
}

public class GetPlansHandler(IAppDbContext context) : IRequestHandler<GetPlansQuery, IEnumerable<PlanDto>>
{
    public async Task<IEnumerable<PlanDto>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        return await context.SubscriptionPlans
            .Select(p => new PlanDto(p.Id, p.Name, p.MaxAdmins, p.MaxSales, p.Price,
                context.Tenants.Count(t => t.SubscriptionPlanId == p.Id)))
            .ToListAsync(cancellationToken);
    }
}

public class UpdatePlanHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<UpdatePlanCommand, PlanDto>
{
    public async Task<PlanDto> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != "SuperAdmin") throw new UnauthorizedAccessException("Only SuperAdmin can manage plans.");

        var plan = await context.SubscriptionPlans.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Plan not found.");

        plan.Name      = request.Name.Trim();
        plan.MaxAdmins = request.MaxAdmins;
        plan.MaxSales  = request.MaxSales;
        plan.Price     = request.Price;

        await context.SaveChangesAsync(cancellationToken);
        var tenantCount = await context.Tenants.CountAsync(t => t.SubscriptionPlanId == plan.Id, cancellationToken);
        return new PlanDto(plan.Id, plan.Name, plan.MaxAdmins, plan.MaxSales, plan.Price, tenantCount);
    }
}

public class DeletePlanHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<DeletePlanCommand, bool>
{
    public async Task<bool> Handle(DeletePlanCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != "SuperAdmin") throw new UnauthorizedAccessException("Only SuperAdmin can manage plans.");

        var plan = await context.SubscriptionPlans
            .Include(p => p.Tenants)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Plan not found.");

        if (plan.Tenants.Any())
            throw new InvalidOperationException($"Cannot delete plan '{plan.Name}' because it has {plan.Tenants.Count} tenant(s) using it.");

        context.SubscriptionPlans.Remove(plan);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class GetTenantByIdHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<GetTenantByIdQuery, TenantDetailDto>
{
    public async Task<TenantDetailDto> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != "SuperAdmin") throw new UnauthorizedAccessException("Only SuperAdmin can view tenant details.");

        var tenant = await context.Tenants
            .Include(t => t.SubscriptionPlan)
            .Include(t => t.Users)
            .Include(t => t.Companies)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Tenant not found.");

        var orderCount = await context.SalesOrders.IgnoreQueryFilters()
            .CountAsync(o => o.TenantId == tenant.Id, cancellationToken);

        return new TenantDetailDto(
            tenant.Id, tenant.Name, tenant.Industry,
            tenant.SubscriptionPlanId, tenant.SubscriptionPlan!.Name,
            tenant.IsActive, tenant.SubscriptionStart, tenant.SubscriptionEnd,
            tenant.Users.Count(u => u.Role == Role.Admin),
            tenant.Users.Count(u => u.Role == Role.Sales),
            tenant.Companies.Count,
            orderCount);
    }
}

public class UpdateTenantHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<UpdateTenantCommand, TenantDto>
{
    public async Task<TenantDto> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != "SuperAdmin") throw new UnauthorizedAccessException("Only SuperAdmin can manage tenants.");

        var tenant = await context.Tenants
            .Include(t => t.SubscriptionPlan)
            .Include(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Tenant not found.");

        var plan = await context.SubscriptionPlans.FindAsync([request.SubscriptionPlanId], cancellationToken)
            ?? throw new KeyNotFoundException("Plan not found.");

        // Downgrade safety: check if new plan allows current user counts
        var adminCount = tenant.Users.Count(u => u.Role == Role.Admin);
        var salesCount = tenant.Users.Count(u => u.Role == Role.Sales);
        if (adminCount > plan.MaxAdmins)
            throw new InvalidOperationException($"Cannot downgrade: tenant has {adminCount} admins but new plan allows only {plan.MaxAdmins}.");
        if (salesCount > plan.MaxSales)
            throw new InvalidOperationException($"Cannot downgrade: tenant has {salesCount} sales users but new plan allows only {plan.MaxSales}.");

        tenant.Name               = request.Name.Trim();
        tenant.Industry           = request.Industry.Trim();
        tenant.SubscriptionPlanId = request.SubscriptionPlanId;
        tenant.SubscriptionEnd    = request.SubscriptionEnd;

        await context.SaveChangesAsync(cancellationToken);
        return new TenantDto(tenant.Id, tenant.Name, tenant.Industry, tenant.SubscriptionPlanId, plan.Name, tenant.IsActive, tenant.SubscriptionStart, tenant.SubscriptionEnd, tenant.Users.Count);
    }
}

public class GetSuperAdminDashboardHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<GetSuperAdminDashboardQuery, SuperAdminDashboardDto>
{
    public async Task<SuperAdminDashboardDto> Handle(GetSuperAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != "SuperAdmin") throw new UnauthorizedAccessException("Only SuperAdmin can access this dashboard.");

        var tenants = await context.Tenants
            .Include(t => t.SubscriptionPlan)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var allUsers = await context.Users.IgnoreQueryFilters()
            .Where(u => u.Role != Role.SuperAdmin)
            .ToListAsync(cancellationToken);

        var plans = await context.SubscriptionPlans.ToListAsync(cancellationToken);

        var totalRevenue = tenants
            .Where(t => t.IsActive)
            .Sum(t => t.SubscriptionPlan?.Price ?? 0);

        var recentTenants = tenants.Take(10)
            .Select(t => new TenantSummaryDto(t.Id, t.Name, t.SubscriptionPlan?.Name ?? "-", t.IsActive, t.CreatedAt));

        var planUsage = plans.Select(p => new PlanUsageDto(
            p.Name,
            tenants.Count(t => t.SubscriptionPlanId == p.Id),
            tenants.Where(t => t.SubscriptionPlanId == p.Id && t.IsActive).Sum(_ => p.Price)));

        return new SuperAdminDashboardDto(
            tenants.Count,
            tenants.Count(t => t.IsActive),
            tenants.Count(t => !t.IsActive),
            allUsers.Count,
            allUsers.Count(u => u.Role == Role.Admin),
            allUsers.Count(u => u.Role == Role.Sales),
            plans.Count,
            totalRevenue,
            recentTenants,
            planUsage);
    }
}

public class GetAuditLogsHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<GetAuditLogsQuery, IEnumerable<AuditLogDto>>
{
    public async Task<IEnumerable<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != "SuperAdmin" && currentUser.Role != "Admin")
            throw new UnauthorizedAccessException("Access denied.");

        var query = context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityName))
            query = query.Where(a => a.EntityName == request.EntityName);

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action == request.Action);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogDto(a.Id, a.EntityName, a.EntityId, a.Action,
                a.OldValues, a.NewValues, a.UserName, a.IpAddress, a.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

public class GetMyProfileHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<GetMyProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Not authenticated.");
        var user = await context.Users.IgnoreQueryFilters()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        return new UserProfileDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.TenantId, user.Tenant?.Name, user.AvatarUrl, user.Phone);
    }
}

public class UpdateMyProfileHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<UpdateMyProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Not authenticated.");
        var user = await context.Users.IgnoreQueryFilters()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@')) throw new ArgumentException("Valid email required.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (email != user.Email && await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email && u.Id != userId, cancellationToken))
            throw new ArgumentException("Email already in use.");

        user.Name  = request.Name.Trim();
        user.Email = email;
        user.Phone = request.Phone?.Trim();
        user.AvatarUrl = request.AvatarUrl?.Trim();

        await context.SaveChangesAsync(cancellationToken);
        return new UserProfileDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.TenantId, user.Tenant?.Name, user.AvatarUrl, user.Phone);
    }
}

public class ChangeMyPasswordHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<ChangeMyPasswordCommand, bool>
{
    public async Task<bool> Handle(ChangeMyPasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Not authenticated.");
        var user = await context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ArgumentException("Current password is incorrect.");

        if (request.NewPassword != request.ConfirmPassword)
            throw new ArgumentException("Passwords do not match.");

        if (request.NewPassword.Length < 8)
            throw new ArgumentException("New password must be at least 8 characters.");

        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            throw new ArgumentException("New password must be different from current password.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        // Revoke all refresh tokens for this user (force re-login on all devices)
        var userTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var t in userTokens)
        {
            t.IsRevoked = true;
            t.RevokedByIp = "PasswordChange";
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
