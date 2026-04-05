using Domain.Entities;

namespace Domain.Interfaces;

public interface ICompanyRepository : IGenericRepository<Company>
{
    Task<(IEnumerable<Company> Items, int TotalCount)> SearchAsync(
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
        int size);
}
