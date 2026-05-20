using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

/// <summary>
/// Application-level abstraction over the DbContext.
/// Infrastructure implements this — Application only sees the interface.
/// </summary>
public interface IAppDbContext
{
    DbSet<SystemUser> Users { get; }
    DbSet<Company> Companies { get; }
    DbSet<CompanyContact> CompanyContacts { get; }
    DbSet<Activity> Activities { get; }
    DbSet<TechSolution> TechSolutions { get; }
    DbSet<SalesOrder> SalesOrders { get; }
    DbSet<SalesOrderItem> SalesOrderItems { get; }
    DbSet<ExchangeRate> ExchangeRates { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<ImportLog> ImportLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DbSet<Tenant> Tenants { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<UserSmtpSetting> UserSmtpSettings { get; }

    // B-1 — Notifications
    DbSet<Notification> Notifications { get; }

    // D-5 — Company Tags
    DbSet<CompanyTag> CompanyTags { get; }
    DbSet<CompanyTagAssignment> CompanyTagAssignments { get; }

    // D-6 — Company Notes
    DbSet<CompanyNote> CompanyNotes { get; }

    // E-2 — Stage History
    DbSet<StageHistory> StageHistories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

