using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CompanyRepository(AppDbContext context)
    : GenericRepository<Company>(context), ICompanyRepository
{
    public async Task<(IEnumerable<Company> Items, int TotalCount)> SearchAsync(
        string? name,
        string? country,
        int? safaKey,
        string? email,
        string? phone,
        string? accountType,
        string? stage,
        string? leadStatus,
        Guid? assignedToUserId,
        Guid? currentUserId,
        bool isAdmin,
        int page,
        int size)
    {
        var query = _dbSet
            .Include(c => c.AssignedTo)
            .Where(c => c.IsActive)
            .AsQueryable();

        // Ownership filter — Sales can only see their own companies
        if (!isAdmin && currentUserId.HasValue)
            query = query.Where(c => c.AssignedToUserId == currentUserId.Value);

        // Filters
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(c =>
                c.ArabicName.Contains(name) ||
                c.EnglishName.Contains(name));

        if (!string.IsNullOrWhiteSpace(country))
            query = query.Where(c => c.Country.Contains(country));

        if (safaKey.HasValue)
            query = query.Where(c => c.SafaKey == safaKey.Value);

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(c => c.Email.Contains(email));

        if (!string.IsNullOrWhiteSpace(phone))
            query = query.Where(c => c.Phone.Contains(phone));

        if (!string.IsNullOrWhiteSpace(accountType))
            query = query.Where(c => c.AccountType == accountType);

        if (!string.IsNullOrWhiteSpace(stage) &&
            Enum.TryParse<Domain.Enums.Stage>(stage, true, out var parsedStage))
            query = query.Where(c => c.Stage == parsedStage);

        if (!string.IsNullOrWhiteSpace(leadStatus))
            query = query.Where(c => c.LeadStatus == leadStatus);

        // Admin-only: filter by assigned sales rep
        if (isAdmin && assignedToUserId.HasValue)
            query = query.Where(c => c.AssignedToUserId == assignedToUserId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, totalCount);
    }
}
