using Application.Common.Interfaces;
using Application.Features.Auth.Commands;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;

namespace Application.Features.Auth.Handlers;


public class LoginHandler(IUserRepository userRepo, IJwtService jwtService)
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
        var refreshToken = jwtService.GenerateRefreshToken();
        var expiry = jwtService.RefreshTokenExpiry();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = expiry;
        userRepo.Update(user);
        await userRepo.SaveChangesAsync();

        return new AuthResponse(
            accessToken, refreshToken,
            DateTime.UtcNow.AddMinutes(60),
            user.Id, user.Name, user.Email, user.Role.ToString());
    }
}

public class RefreshTokenHandler(IUserRepository userRepo, IJwtService jwtService)
    : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var user = await userRepo.GetByRefreshTokenAsync(request.RefreshToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        var accessToken = jwtService.GenerateAccessToken(user);
        var newRefresh = jwtService.GenerateRefreshToken();
        var expiry = jwtService.RefreshTokenExpiry();

        user.RefreshToken = newRefresh;
        user.RefreshTokenExpiry = expiry;
        userRepo.Update(user);
        await userRepo.SaveChangesAsync();

        return new AuthResponse(
            accessToken, newRefresh,
            DateTime.UtcNow.AddMinutes(60),
            user.Id, user.Name, user.Email, user.Role.ToString());
    }
}

public class LogoutHandler(IUserRepository userRepo)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var user = await userRepo.GetByIdAsync(request.UserId);
        if (user is null) return;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        userRepo.Update(user);
        await userRepo.SaveChangesAsync();
    }
}

// ── Register ─────────────────────────────────────────────────────────────────
public class RegisterHandler(IUserRepository userRepo, IJwtService jwtService)
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

        // Issue tokens immediately so registration = logged in
        var accessToken  = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();
        var expiry       = jwtService.RefreshTokenExpiry();

        user.RefreshToken       = refreshToken;
        user.RefreshTokenExpiry = expiry;
        userRepo.Update(user);
        await userRepo.SaveChangesAsync();

        return new AuthResponse(
            accessToken, refreshToken,
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
public class ResetPasswordHandler(IUserRepository userRepo)
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
        user.RefreshToken       = null;
        user.RefreshTokenExpiry = null;

        userRepo.Update(user);
        await userRepo.SaveChangesAsync();
    }
}
