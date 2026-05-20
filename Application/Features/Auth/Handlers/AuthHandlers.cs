using Application.Common.Interfaces;
using Application.Features.Auth.Commands;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Handlers;


public class LoginHandler(IUserRepository userRepo, IJwtService jwtService, IAppDbContext dbContext)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userRepo.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshTokenString = jwtService.GenerateRefreshToken();
        var expiry = jwtService.RefreshTokenExpiry();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = expiry,
            CreatedByIp = request.IpAddress
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken, refreshTokenString,
            DateTime.UtcNow.AddMinutes(60),
            user.Id, user.Name, user.Email, user.Role.ToString());
    }
}

public class RefreshTokenHandler(IUserRepository userRepo, IJwtService jwtService, IAppDbContext dbContext)
    : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var refreshToken = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        var user = refreshToken.User;

        // If the token is revoked, it's a security breach. Revoke all tokens for this user.
        if (refreshToken.IsRevoked)
        {
            var userTokens = await dbContext.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync(ct);
            
            foreach (var t in userTokens)
            {
                t.IsRevoked = true;
                t.RevokedByIp = request.IpAddress;
            }
            await dbContext.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Compromised token detected. All sessions revoked.");
        }

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        // Valid token. Revoke it and generate a new one.
        refreshToken.IsRevoked = true;
        refreshToken.RevokedByIp = request.IpAddress;

        var accessToken = jwtService.GenerateAccessToken(user);
        var newRefreshString = jwtService.GenerateRefreshToken();
        var expiry = jwtService.RefreshTokenExpiry();

        refreshToken.ReplacedByToken = newRefreshString;

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshString,
            ExpiresAt = expiry,
            CreatedByIp = request.IpAddress
        };

        dbContext.RefreshTokens.Add(newRefreshToken);
        await dbContext.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken, newRefreshString,
            DateTime.UtcNow.AddMinutes(60),
            user.Id, user.Name, user.Email, user.Role.ToString());
    }
}

public class LogoutHandler(IAppDbContext dbContext)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        // Find all active tokens for the user and revoke them
        var activeTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == request.UserId && !rt.IsRevoked)
            .ToListAsync(ct);

        foreach (var t in activeTokens)
        {
            t.IsRevoked = true;
            t.RevokedByIp = request.IpAddress;
        }

        if (activeTokens.Any())
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }
}

// ── Register ─────────────────────────────────────────────────────────────────
public class RegisterHandler(IUserRepository userRepo, IJwtService jwtService, IAppDbContext dbContext)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;

        if (r.Password != r.ConfirmPassword)
            throw new ArgumentException("Passwords do not match.");

        var existing = await userRepo.GetByEmailAsync(r.Email);
        if (existing is not null)
            throw new InvalidOperationException("An account with this email already exists.");

        if (!Enum.TryParse<Role>(r.Role, true, out var role))
            role = Role.Sales;

        var user = new SystemUser
        {
            Name     = r.Name,
            Email    = r.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(r.Password),
            Role     = role,
            IsActive = true
        };

        await userRepo.AddAsync(user);
        await userRepo.SaveChangesAsync(); // Save user to get ID

        // Issue tokens immediately so registration = logged in
        var accessToken  = jwtService.GenerateAccessToken(user);
        var refreshTokenString = jwtService.GenerateRefreshToken();
        var expiry       = jwtService.RefreshTokenExpiry();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = expiry,
            CreatedByIp = cmd.IpAddress
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken, refreshTokenString,
            DateTime.UtcNow.AddMinutes(60),
            user.Id, user.Name, user.Email, user.Role.ToString());
    }
}

// ── Forgot Password ───────────────────────────────────────────────────────────
public class ForgotPasswordHandler(IUserRepository userRepo, IEmailService emailService)
    : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        // Always return 204 to avoid user enumeration — even if email not found
        var user = await userRepo.GetByEmailAsync(cmd.Email);
        if (user is null) return;

        // Secure random token (hex string)
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        user.PasswordResetToken       = BCrypt.Net.BCrypt.HashPassword(token);
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        userRepo.Update(user);
        await userRepo.SaveChangesAsync();

        // Build reset link — frontend will navigate to reset page with token + email
        var resetLink =
            $"https://your-frontend.com/reset-password?token={token}&email={Uri.EscapeDataString(user.Email)}";

        var html = $"""
            <h2>Password Reset Request</h2>
            <p>Hello {user.Name},</p>
            <p>We received a request to reset your password. Click the button below to set a new password.
               This link expires in <strong>15 minutes</strong>.</p>
            <p style="margin:24px 0">
              <a href="{resetLink}"
                 style="background:#2563eb;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:600">
                Reset Password
              </a>
            </p>
            <p>If you didn't request this, you can safely ignore this email.</p>
            """;

        await emailService.SendAsync(user.Email, user.Name, "Reset Your CRM Password", html);
    }
}

// ── Reset Password ────────────────────────────────────────────────────────────
public class ResetPasswordHandler(IUserRepository userRepo, IAppDbContext dbContext)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;

        if (r.NewPassword != r.ConfirmPassword)
            throw new ArgumentException("Passwords do not match.");

        var user = await userRepo.GetByEmailAsync(r.Email)
            ?? throw new ArgumentException("Invalid reset request.");

        if (user.PasswordResetToken is null || user.PasswordResetTokenExpiry is null)
            throw new ArgumentException("No password reset was requested for this account.");

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            throw new ArgumentException("Reset link has expired. Please request a new one.");

        if (!BCrypt.Net.BCrypt.Verify(r.Token, user.PasswordResetToken))
            throw new ArgumentException("Invalid or already-used reset token.");

        // Set new password and clear the reset token
        user.PasswordHash             = BCrypt.Net.BCrypt.HashPassword(r.NewPassword);
        user.PasswordResetToken       = null;
        user.PasswordResetTokenExpiry = null;

        // Also invalidate any active sessions
        var activeTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync(ct);

        foreach (var t in activeTokens)
        {
            t.IsRevoked = true;
            t.RevokedByIp = "SystemReset";
        }

        userRepo.Update(user);
        await dbContext.SaveChangesAsync(ct);
        await userRepo.SaveChangesAsync();
    }
}

public class GetMeHandler(IAppDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<GetMeQuery, MeResponse>
{
    public async Task<MeResponse> Handle(GetMeQuery q, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Not authenticated.");

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.SmtpSetting)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        var unreadCount = await dbContext.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

        return new MeResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role.ToString(),
            user.SmtpSetting != null,
            unreadCount);
    }
}

