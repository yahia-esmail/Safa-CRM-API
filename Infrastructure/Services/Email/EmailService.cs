using Application.Common.Interfaces;
using Infrastructure.Persistence;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Infrastructure.Services.Email;

public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}

public class EmailService(
    IConfiguration configuration,
    ILogger<EmailService> logger,
    IAppDbContext dbContext,
    ICurrentUserService currentUserService,
    IEncryptionService encryptionService)
{
    private readonly EmailSettings _fallbackSettings = configuration.GetSection("Email").Get<EmailSettings>()
        ?? new EmailSettings();

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var userId = currentUserService.UserId;
            var smtpSetting = userId.HasValue
                ? await dbContext.UserSmtpSettings.FirstOrDefaultAsync(s => s.UserId == userId.Value)
                : null;

            var host = smtpSetting?.Host ?? _fallbackSettings.Host;
            var port = smtpSetting?.Port ?? _fallbackSettings.Port;
            var username = smtpSetting?.Email ?? _fallbackSettings.Username;
            var password = smtpSetting != null
                ? encryptionService.Decrypt(smtpSetting.Password)
                : _fallbackSettings.Password;
            var fromName = currentUserService.Role != null ? "Safa CRM User" : _fallbackSettings.FromName;
            var fromEmail = username;

            if (string.IsNullOrEmpty(host))
                throw new InvalidOperationException("No SMTP settings found for this user and no fallback configured.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();
            var options = smtpSetting?.Encryption?.ToUpperInvariant() switch
            {
                "SSL" => SecureSocketOptions.SslOnConnect,
                "NONE" => SecureSocketOptions.None,
                _ => SecureSocketOptions.StartTls
            };

            await smtpClient.ConnectAsync(host, port, options);
            await smtpClient.AuthenticateAsync(username, password);
            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true);

            logger.LogInformation("Email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendBulkAsync(IEnumerable<(string Email, string Name)> recipients, string subject, string htmlBody)
    {
        var tasks = recipients.Select(r => SendAsync(r.Email, r.Name, subject, htmlBody));
        await Task.WhenAll(tasks);
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(
        string host,
        int port,
        string email,
        string password,
        string encryption)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Safa CRM SMTP Test", email));
            message.To.Add(new MailboxAddress("Self", email));
            message.Subject = "Safa CRM — SMTP Configuration Test";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $"""
                    <h3>SMTP Connection Successful!</h3>
                    <p>This email confirms that Safa CRM was able to successfully authenticate and connect using your SMTP settings.</p>
                    <ul>
                        <li><strong>Host:</strong> {host}</li>
                        <li><strong>Port:</strong> {port}</li>
                        <li><strong>Encryption:</strong> {encryption}</li>
                    </ul>
                    <p>Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                    """
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();
            var options = encryption.ToUpperInvariant() switch
            {
                "SSL" => SecureSocketOptions.SslOnConnect,
                "NONE" => SecureSocketOptions.None,
                _ => SecureSocketOptions.StartTls
            };

            await smtpClient.ConnectAsync(host, port, options);
            await smtpClient.AuthenticateAsync(email, password);
            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true);

            return (true, $"Test email sent successfully to {email}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP configuration test failed for {Email} at {Host}:{Port}", email, host, port);
            return (false, $"Connection failed: {ex.Message}");
        }
    }
}
