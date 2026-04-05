using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository(AppDbContext context)
    : GenericRepository<SystemUser>(context), IUserRepository
{
    public async Task<SystemUser?> GetByEmailAsync(string email) =>
        await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<SystemUser?> GetByRefreshTokenAsync(string refreshToken) =>
        await _dbSet.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
}
