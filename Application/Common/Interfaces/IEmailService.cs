namespace Application.Common.Interfaces;

/// <summary>Application-level email abstraction</summary>
public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
    Task SendBulkAsync(IEnumerable<(string Email, string Name)> recipients, string subject, string htmlBody);
}
