using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SmtpSettings;

// ── DTOs ────────────────────────────────────────────────────────────────────────

public record SmtpSettingDto(
    string Host,
    int Port,
    string Email,
    string Encryption,
    DateTime UpdatedAt);

public record CreateOrUpdateSmtpRequest(
    string Host,
    int Port,
    string Email,
    string Password,
    string Encryption);

public record TestSmtpRequest(
    string? Host,
    int? Port,
    string? Email,
    string? Password,
    string? Encryption);

public record TestSmtpResult(bool Success, string Message);

// ── Commands & Queries ──────────────────────────────────────────────────────────

public record GetSmtpQuery : IRequest<SmtpSettingDto>;
public record CreateOrUpdateSmtpCommand(CreateOrUpdateSmtpRequest Request) : IRequest<SmtpSettingDto>;
public record DeleteSmtpCommand : IRequest;
public record TestSmtpCommand(TestSmtpRequest Request) : IRequest<TestSmtpResult>;

// ── Handlers ────────────────────────────────────────────────────────────────────

public class GetSmtpHandler(IAppDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetSmtpQuery, SmtpSettingDto>
{
    public async Task<SmtpSettingDto> Handle(GetSmtpQuery q, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Not authenticated.");

        var setting = await context.UserSmtpSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, ct)
            ?? throw new KeyNotFoundException("SMTP settings not configured for this user.");

        return new SmtpSettingDto(
            setting.Host,
            setting.Port,
            setting.Email,
            setting.Encryption,
            setting.UpdatedAt);
    }
}

public class CreateOrUpdateSmtpHandler(
    IAppDbContext context,
    ICurrentUserService currentUser,
    IEncryptionService encryptionService)
    : IRequestHandler<CreateOrUpdateSmtpCommand, SmtpSettingDto>
{
    public async Task<SmtpSettingDto> Handle(CreateOrUpdateSmtpCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Not authenticated.");
        var r = cmd.Request;

        if (string.IsNullOrWhiteSpace(r.Host)) throw new ArgumentException("Host is required.");
        if (r.Port <= 0) throw new ArgumentException("Valid Port is required.");
        if (string.IsNullOrWhiteSpace(r.Email) || !r.Email.Contains('@')) throw new ArgumentException("Valid Email is required.");
        if (string.IsNullOrWhiteSpace(r.Password)) throw new ArgumentException("Password is required.");

        var encryptedPassword = encryptionService.Encrypt(r.Password);

        var setting = await context.UserSmtpSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        var now = DateTime.UtcNow;
        if (setting == null)
        {
            setting = new UserSmtpSetting
            {
                UserId = userId,
                Host = r.Host.Trim(),
                Port = r.Port,
                Email = r.Email.Trim().ToLowerInvariant(),
                Password = encryptedPassword,
                Encryption = r.Encryption ?? "STARTTLS",
                UpdatedAt = now
            };
            context.UserSmtpSettings.Add(setting);
        }
        else
        {
            setting.Host = r.Host.Trim();
            setting.Port = r.Port;
            setting.Email = r.Email.Trim().ToLowerInvariant();
            setting.Password = encryptedPassword;
            setting.Encryption = r.Encryption ?? "STARTTLS";
            setting.UpdatedAt = now;
        }

        await context.SaveChangesAsync(ct);

        return new SmtpSettingDto(
            setting.Host,
            setting.Port,
            setting.Email,
            setting.Encryption,
            setting.UpdatedAt);
    }
}

public class DeleteSmtpHandler(IAppDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<DeleteSmtpCommand>
{
    public async Task Handle(DeleteSmtpCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Not authenticated.");

        var setting = await context.UserSmtpSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, ct)
            ?? throw new KeyNotFoundException("SMTP settings not found.");

        context.UserSmtpSettings.Remove(setting);
        await context.SaveChangesAsync(ct);
    }
}

public class TestSmtpHandler(
    IAppDbContext context,
    ICurrentUserService currentUser,
    IEncryptionService encryptionService,
    IEmailService emailService)
    : IRequestHandler<TestSmtpCommand, TestSmtpResult>
{
    public async Task<TestSmtpResult> Handle(TestSmtpCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Not authenticated.");
        var r = cmd.Request;

        string host;
        int port;
        string email;
        string password;
        string encryption;

        // If request parameters are provided, test them on the fly
        if (!string.IsNullOrWhiteSpace(r.Host) && r.Port.HasValue && !string.IsNullOrWhiteSpace(r.Email) && !string.IsNullOrWhiteSpace(r.Password))
        {
            host = r.Host;
            port = r.Port.Value;
            email = r.Email;
            password = r.Password;
            encryption = r.Encryption ?? "STARTTLS";
        }
        else
        {
            // Otherwise test the saved database configuration
            var setting = await context.UserSmtpSettings
                .FirstOrDefaultAsync(s => s.UserId == userId, ct)
                ?? throw new KeyNotFoundException("SMTP settings not configured. Please supply parameters or configure them first.");

            host = setting.Host;
            port = setting.Port;
            email = setting.Email;
            password = encryptionService.Decrypt(setting.Password);
            encryption = setting.Encryption;
        }

        var (success, message) = await emailService.TestConnectionAsync(host, port, email, password, encryption);
        return new TestSmtpResult(success, message);
    }
}

