using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SalesOrderRepository(AppDbContext context)
    : GenericRepository<SalesOrder>(context), ISalesOrderRepository
{
    public async Task<IEnumerable<SalesOrder>> GetByCompanyAsync(Guid companyId) =>
        await _dbSet
            .Include(o => o.Items).ThenInclude(i => i.Solution)
            .Include(o => o.CreatedBy)
            .Where(o => o.CompanyId == companyId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<SalesOrder>> GetByUserAsync(Guid userId) =>
        await _dbSet
            .Include(o => o.Company)
            .Include(o => o.Items).ThenInclude(i => i.Solution)
            .Where(o => o.CreatedByUserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    /// <summary>
    /// Generates the next sequential invoice number in format INV/{year}/{seq:D5}.
    /// Uses a DB-level MAX to ensure uniqueness even under concurrent access.
    /// </summary>
    public async Task<string> GenerateInvoiceNumberAsync(int year)
    {
        var prefix = $"INV/{year}/";

        var lastSeq = await _dbSet
            .Where(o => o.InvoiceNumber.StartsWith(prefix))
            .Select(o => o.InvoiceNumber.Substring(prefix.Length))
            .Select(seq => int.Parse(seq))
            .DefaultIfEmpty(0)
            .MaxAsync();

        return $"{prefix}{(lastSeq + 1):D5}";
    }
}
