using MailKit.Net.Smtp;
using MailKit.Security;
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

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger)
{
    private readonly EmailSettings _settings = configuration.GetSection("Email").Get<EmailSettings>()
        ?? throw new InvalidOperationException("Email settings not configured.");

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            await smtpClient.AuthenticateAsync(_settings.Username, _settings.Password);
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
}
