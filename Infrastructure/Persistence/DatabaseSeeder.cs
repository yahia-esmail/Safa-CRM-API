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

        // 1. Ensure Migrations are applied
        await context.Database.MigrateAsync();

        // 2. Seed Subscription Plans
        if (!await context.SubscriptionPlans.AnyAsync())
        {
            var plans = new List<SubscriptionPlan>
            {
                new() { Name = "Basic", MaxAdmins = 1, MaxSales = 3, Price = 100, CreatedAt = DateTime.UtcNow },
                new() { Name = "Pro", MaxAdmins = 3, MaxSales = 10, Price = 300, CreatedAt = DateTime.UtcNow },
                new() { Name = "Enterprise", MaxAdmins = 10, MaxSales = 50, Price = 1000, CreatedAt = DateTime.UtcNow }
            };
            context.SubscriptionPlans.AddRange(plans);
            await context.SaveChangesAsync();
        }

        // 3. Seed Default Tenant
        Tenant? defaultTenant = await context.Tenants.FirstOrDefaultAsync();
        if (defaultTenant == null)
        {
            var proPlan = await context.SubscriptionPlans.FirstAsync(p => p.Name == "Pro");
            defaultTenant = new Tenant
            {
                Name = "Safa Soft",
                Industry = "Travel & Tourism",
                SubscriptionPlanId = proPlan.Id,
                SubscriptionStart = DateTime.UtcNow,
                SubscriptionEnd = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Tenants.Add(defaultTenant);
            await context.SaveChangesAsync();
        }

        // 4. Seed Users (SuperAdmin, Admin, Sales)
        if (!await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == "super@safa.com"))
        {
            var superAdmin = new SystemUser
            {
                Name = "Super Admin",
                Email = "super@safa.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Super@123"),
                Role = Role.SuperAdmin,
                IsActive = true,
                TenantId = null
            };
            context.Users.Add(superAdmin);
        }

        if (!await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == "admin@safasoft.com"))
        {
            var adminUser = new SystemUser
            {
                Name = "Yahia Ismiel",
                Email = "admin@safasoft.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = Role.Admin,
                IsActive = true,
                TenantId = defaultTenant?.Id
            };
            context.Users.Add(adminUser);
        }

        if (!await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == "sales@safasoft.com"))
        {
            var salesUser = new SystemUser
            {
                Name = "Sales Manager",
                Email = "sales@safasoft.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sales@123"),
                Role = Role.Sales,
                IsActive = true,
                TenantId = defaultTenant?.Id
            };
            context.Users.Add(salesUser);
        }

        await context.SaveChangesAsync();
    }
}
