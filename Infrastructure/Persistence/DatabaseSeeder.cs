using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

/// <summary>
/// Pre-populates the database with foundational data, like an initial Admin user.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();

        if (!await context.Users.AnyAsync())
        {
            var adminUser = new SystemUser
            {
                Name = "System Administrator",
                Email = "admin@safa-crm.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), // Default password
                Role = Role.Admin,
                IsActive = true
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }
    }
}
