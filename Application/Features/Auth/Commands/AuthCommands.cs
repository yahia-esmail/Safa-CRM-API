using MediatR;

namespace Application.Features.Auth.Commands;

// --- DTOs ---
public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    Guid UserId,
    string Name,
    string Email,
    string Role);

public record RefreshTokenRequest(string RefreshToken);

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string ConfirmPassword,
    string Role = "Sales");

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(
    string Token,
    string Email,
    string NewPassword,
    string ConfirmPassword);

// --- Commands ---
public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;
public record LogoutCommand(Guid UserId) : IRequest;
public record RegisterCommand(RegisterRequest Request) : IRequest<AuthResponse>;
public record ForgotPasswordCommand(string Email) : IRequest;
public record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest;

