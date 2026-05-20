using Domain.Entities;

namespace Domain.Interfaces;

public interface IUserRepository : IGenericRepository<SystemUser>
{
    Task<SystemUser?> GetByEmailAsync(string email);
}
