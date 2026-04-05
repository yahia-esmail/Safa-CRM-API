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
    DbSet<ImportLog> ImportLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
