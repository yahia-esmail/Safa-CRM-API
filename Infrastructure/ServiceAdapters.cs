using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Email;
using Infrastructure.Services.ExchangeRate;

namespace Infrastructure;

// Adapter: EmailService → IEmailService
public class EmailServiceAdapter(EmailService inner) : IEmailService
{
    public Task SendAsync(string toEmail, string toName, string subject, string htmlBody) =>
        inner.SendAsync(toEmail, toName, subject, htmlBody);

    public Task SendBulkAsync(IEnumerable<(string Email, string Name)> recipients, string subject, string htmlBody) =>
        inner.SendBulkAsync(recipients, subject, htmlBody);
}

// Adapter: JwtService → IJwtService
public class JwtServiceAdapter(JwtService inner) : IJwtService
{
    public string GenerateAccessToken(SystemUser user) => inner.GenerateAccessToken(user);
    public string GenerateRefreshToken() => inner.GenerateRefreshToken();
    public DateTime RefreshTokenExpiry() => inner.RefreshTokenExpiry();
}

// Adapter: ExchangeRateService → IExchangeRateFetcher
public class ExchangeRateFetcherAdapter(ExchangeRateService inner) : IExchangeRateFetcher
{
    public Task FetchAndSaveRatesAsync() => inner.FetchAndSaveRatesAsync();
}
