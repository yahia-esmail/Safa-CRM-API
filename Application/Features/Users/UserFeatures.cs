using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users;

// --- DTOs ---
public record UserDto(Guid Id, string Name, string Email, string Role, bool IsActive, DateTime CreatedAt, Guid? TenantId);
public record CreateUserRequest(string Name, string Email, string Password, string Role, Guid? TenantId = null);
public record UpdateUserRequest(string Name, string Email, string Role, bool IsActive);

// --- Commands & Queries ---
public record GetUsersQuery : IRequest<IEnumerable<UserDto>>;
public record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;
public record CreateUserCommand(CreateUserRequest Request) : IRequest<UserDto>;
public record UpdateUserCommand(Guid Id, UpdateUserRequest Request) : IRequest<UserDto>;
public record DeleteUserCommand(Guid Id) : IRequest<bool>;
public record ToggleUserStatusCommand(Guid Id, bool IsActive) : IRequest<UserDto>;
public record GetUserCompaniesQuery(Guid UserId) : IRequest<IEnumerable<Application.Features.Companies.CompanyDto>>;


// --- Handlers ---
public class GetUsersHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<GetUsersQuery, IEnumerable<UserDto>>
{
    public async Task<IEnumerable<UserDto>> Handle(GetUsersQuery q, CancellationToken ct)
    {
        var query = context.Users.AsQueryable();
        
        // SuperAdmin sees all. Others only see their Tenant's users.
        // But EF Core Global Query Filter already handles this!
        
        return await query
            .OrderBy(u => u.Name)
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt, u.TenantId))
            .ToListAsync(ct);
    }
}

public class GetUserByIdHandler(IAppDbContext context) : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery q, CancellationToken ct)
    {
        var u = await context.Users.FindAsync([q.Id], ct)
            ?? throw new KeyNotFoundException("User not found.");
        return new UserDto(u.Id, u.Name, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt, u.TenantId);
    }
}

public class CreateUserHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;

        // --- Input validation ---
        if (string.IsNullOrWhiteSpace(r.Name)) throw new ArgumentException("Name is required / الاسم مطلوب.");
        if (string.IsNullOrWhiteSpace(r.Email) || !r.Email.Contains('@')) throw new ArgumentException("A valid email address is required / البريد الإلكتروني مطلوب.");
        if (string.IsNullOrWhiteSpace(r.Password) || r.Password.Length < 6) throw new ArgumentException("Password must be at least 6 characters.");
        if (!Enum.TryParse<Role>(r.Role, true, out var role)) throw new ArgumentException($"Invalid role '{r.Role}'.");

        var currentRole = currentUser.Role;
        Guid? tenantIdToAssign = null;

        // --- SAAS Role Logic ---
        if (currentRole == Role.SuperAdmin.ToString())
        {
            if (role != Role.Admin)
                throw new UnauthorizedAccessException("SuperAdmin can only create Admin users.");
            
            if (!r.TenantId.HasValue)
                throw new ArgumentException("TenantId is required when creating an Admin.");
                
            tenantIdToAssign = r.TenantId.Value;
        }
        else if (currentRole == Role.Admin.ToString())
        {
            if (role != Role.Sales)
                throw new UnauthorizedAccessException("Admin can only create Sales users.");
                
            tenantIdToAssign = currentUser.TenantId ?? throw new UnauthorizedAccessException("Admin must belong to a Tenant.");
        }
        else
        {
            throw new UnauthorizedAccessException("You do not have permission to create users.");
        }

        // --- Check Plan Limits ---
        if (tenantIdToAssign.HasValue)
        {
            var tenant = await context.Tenants
                .Include(t => t.SubscriptionPlan)
                .Include(t => t.Users)
                .FirstOrDefaultAsync(t => t.Id == tenantIdToAssign.Value, ct)
                ?? throw new KeyNotFoundException("Tenant not found.");

            if (role == Role.Admin && tenant.Users.Count(u => u.Role == Role.Admin) >= tenant.SubscriptionPlan!.MaxAdmins)
                throw new InvalidOperationException("Tenant has reached the maximum number of Admins allowed by its plan.");
                
            if (role == Role.Sales && tenant.Users.Count(u => u.Role == Role.Sales) >= tenant.SubscriptionPlan!.MaxSales)
                throw new InvalidOperationException("Tenant has reached the maximum number of Sales allowed by its plan.");
        }

        // --- Uniqueness check ---
        var email = r.Email.Trim().ToLowerInvariant();
        if (await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, ct))
            throw new ArgumentException("Email already exists.");

        var user = new SystemUser
        {
            Name         = r.Name.Trim(),
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(r.Password),
            Role         = role,
            TenantId     = tenantIdToAssign
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync(ct);
        return new UserDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt, user.TenantId);
    }
}

public class UpdateUserHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await context.Users.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException("User not found.");

        var r = cmd.Request;

        // Basic validation
        if (string.IsNullOrWhiteSpace(r.Name)) throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(r.Email) || !r.Email.Contains('@')) throw new ArgumentException("Valid email required.");
        if (!Enum.TryParse<Role>(r.Role, true, out var role)) throw new ArgumentException("Invalid role.");

        // SuperAdmin checks
        if (currentUser.Role == Role.SuperAdmin.ToString())
        {
            if (role != Role.Admin) throw new UnauthorizedAccessException("SuperAdmin can only manage Admins.");
            
            // If deactivating Admin, the user asked to deactivate all their Sales. 
            // In our system, Sales are under the Tenant. Deactivating the Admin doesn't automatically deactivate the Sales,
            // UNLESS we explicitly implement it. The requirement says: "عندما يتم إيقاف هذا الـ Admin يتم إيقاف جميع الـ Sales التابعين له تلقائياً."
            // If they mean "Sales under the same Tenant", we do this:
            if (user.Role == Role.Admin && r.IsActive == false && user.IsActive == true)
            {
                var salesUsers = await context.Users.Where(u => u.TenantId == user.TenantId && u.Role == Role.Sales).ToListAsync(ct);
                foreach (var s in salesUsers) s.IsActive = false;
            }
        }
        else if (currentUser.Role == Role.Admin.ToString())
        {
            if (user.Role != Role.Sales) throw new UnauthorizedAccessException("Admin can only manage Sales users.");
        }

        var email = r.Email.Trim().ToLowerInvariant();
        if (email != user.Email && await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, ct))
            throw new ArgumentException("Email already exists.");

        user.Name     = r.Name.Trim();
        user.Email    = email;
        user.Role     = role;
        user.IsActive = r.IsActive;
        
        await context.SaveChangesAsync(ct);
        return new UserDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt, user.TenantId);
    }
}

public class DeleteUserHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<DeleteUserCommand, bool>
{
    public async Task<bool> Handle(DeleteUserCommand cmd, CancellationToken ct)
    {
        if (currentUser.Role != Role.SuperAdmin.ToString() && currentUser.Role != Role.Admin.ToString())
            throw new UnauthorizedAccessException("You do not have permission to delete users.");

        var user = await context.Users.IgnoreQueryFilters()
            .Include(u => u.SalesOrders)
            .FirstOrDefaultAsync(u => u.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException("User not found.");

        // Prevent deleting yourself
        if (user.Id == currentUser.UserId)
            throw new InvalidOperationException("You cannot delete your own account.");

        // Admin can only delete Sales users within the same tenant
        if (currentUser.Role == Role.Admin.ToString())
        {
            if (user.Role != Role.Sales) throw new UnauthorizedAccessException("Admin can only delete Sales users.");
            if (user.TenantId != currentUser.TenantId) throw new UnauthorizedAccessException("User belongs to a different tenant.");
        }

        // Prevent hard-delete if user has sales orders (data integrity)
        if (user.SalesOrders.Any())
            throw new InvalidOperationException($"Cannot delete user '{user.Name}' because they have {user.SalesOrders.Count} sales order(s). Deactivate the user instead.");

        context.Users.Remove(user);
        await context.SaveChangesAsync(ct);
        return true;
    }
}

public class ToggleUserStatusHandler(IAppDbContext context, ICurrentUserService currentUser) : IRequestHandler<ToggleUserStatusCommand, UserDto>
{
    public async Task<UserDto> Handle(ToggleUserStatusCommand cmd, CancellationToken ct)
    {
        var user = await context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException("User not found.");

        // SuperAdmin can toggle any non-SuperAdmin user
        if (currentUser.Role == Role.SuperAdmin.ToString())
        {
            if (user.Role == Role.SuperAdmin) throw new UnauthorizedAccessException("Cannot deactivate another SuperAdmin.");
        }
        else if (currentUser.Role == Role.Admin.ToString())
        {
            if (user.Role != Role.Sales) throw new UnauthorizedAccessException("Admin can only toggle Sales users.");
            if (user.TenantId != currentUser.TenantId) throw new UnauthorizedAccessException("User belongs to a different tenant.");
        }
        else
        {
            throw new UnauthorizedAccessException("You do not have permission.");
        }

        user.IsActive = cmd.IsActive;

        // If deactivating an Admin, cascade-deactivate their tenant's Sales users
        if (user.Role == Role.Admin && !cmd.IsActive)
        {
            var salesUsers = await context.Users.IgnoreQueryFilters()
                .Where(u => u.TenantId == user.TenantId && u.Role == Role.Sales)
                .ToListAsync(ct);
            foreach (var s in salesUsers) s.IsActive = false;
        }

        await context.SaveChangesAsync(ct);
        return new UserDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt, user.TenantId);
    }
}

public class GetUserCompaniesHandler(IAppDbContext context)
    : IRequestHandler<GetUserCompaniesQuery, IEnumerable<Application.Features.Companies.CompanyDto>>
{
    public async Task<IEnumerable<Application.Features.Companies.CompanyDto>> Handle(GetUserCompaniesQuery q, CancellationToken ct)
    {
        var userExists = await context.Users.AnyAsync(u => u.Id == q.UserId, ct);
        if (!userExists) throw new KeyNotFoundException("User not found.");

        return await context.Companies
            .Include(c => c.AssignedTo)
            .Include(c => c.TagAssignments).ThenInclude(ta => ta.Tag)
            .Where(c => c.AssignedToUserId == q.UserId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new Application.Features.Companies.CompanyDto(
                c.Id,
                c.ArabicName,
                c.EnglishName,
                c.Country,
                c.Phone,
                c.Email,
                c.Website,
                c.SafaKey,
                c.AccountType,
                c.Stage.ToString(),
                c.LeadSource,
                c.LeadStatus,
                c.ExpectedRevenue,
                c.IsActive,
                c.AssignedToUserId,
                c.AssignedTo != null ? c.AssignedTo.Name : null,
                c.CreatedAt,
                c.ContractAttachment,
                c.ApplicationForm,
                c.TagAssignments.Select(ta => new Application.Features.Tags.TagDto(
                    ta.Tag.Id, ta.Tag.Name, ta.Tag.Color, ta.Tag.CreatedByUserId, ta.Tag.CreatedBy.Name))))
            .ToListAsync(ct);
    }
}

