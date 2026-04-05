using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ExchangeRateRepository(AppDbContext context)
    : GenericRepository<ExchangeRate>(context), IExchangeRateRepository
{
    public async Task<ExchangeRate?> GetTodayRateAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _dbSet.FirstOrDefaultAsync(r => r.Date == today);
    }

    public async Task<ExchangeRate?> GetLatestRateAsync() =>
        await _dbSet.OrderByDescending(r => r.Date).FirstOrDefaultAsync();

    public async Task<IEnumerable<ExchangeRate>> GetHistoryAsync(DateTime from, DateTime to) =>
        await _dbSet
            .Where(r => r.Date >= from.Date && r.Date <= to.Date)
            .OrderByDescending(r => r.Date)
            .ToListAsync();
}
