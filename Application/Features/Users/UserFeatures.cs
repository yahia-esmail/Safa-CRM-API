using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users;

// --- DTOs ---
public record UserDto(Guid Id, string Name, string Email, string Role, bool IsActive, DateTime CreatedAt);
public record CreateUserRequest(string Name, string Email, string Password, string Role);
public record UpdateUserRequest(string Name, string Email, string Role, bool IsActive);

// --- Commands & Queries ---
public record GetUsersQuery : IRequest<IEnumerable<UserDto>>;
public record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;
public record CreateUserCommand(CreateUserRequest Request) : IRequest<UserDto>;
public record UpdateUserCommand(Guid Id, UpdateUserRequest Request) : IRequest<UserDto>;

// --- Handlers ---
public class GetUsersHandler(IAppDbContext context) : IRequestHandler<GetUsersQuery, IEnumerable<UserDto>>
{
    public async Task<IEnumerable<UserDto>> Handle(GetUsersQuery q, CancellationToken ct) =>
        await context.Users
            .OrderBy(u => u.Name)
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
}

public class GetUserByIdHandler(IAppDbContext context) : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery q, CancellationToken ct)
    {
        var u = await context.Users.FindAsync([q.Id], ct)
            ?? throw new KeyNotFoundException("User not found.");
        return new UserDto(u.Id, u.Name, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt);
    }
}

public class CreateUserHandler(IAppDbContext context) : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        if (!Enum.TryParse<Role>(cmd.Request.Role, true, out var role))
            throw new ArgumentException($"Invalid role: {cmd.Request.Role}");

        var user = new SystemUser
        {
            Name = cmd.Request.Name,
            Email = cmd.Request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Request.Password),
            Role = role
        };
        context.Users.Add(user);
        await context.SaveChangesAsync(ct);
        return new UserDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt);
    }
}

public class UpdateUserHandler(IAppDbContext context) : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await context.Users.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!Enum.TryParse<Role>(cmd.Request.Role, true, out var role))
            throw new ArgumentException($"Invalid role: {cmd.Request.Role}");

        user.Name = cmd.Request.Name;
        user.Email = cmd.Request.Email;
        user.Role = role;
        user.IsActive = cmd.Request.IsActive;
        await context.SaveChangesAsync(ct);
        return new UserDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt);
    }
}
