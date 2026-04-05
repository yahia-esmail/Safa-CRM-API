using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Email;

// --- DTOs ---
public record SendEmailRequest(string ToEmail, string ToName, string Subject, string HtmlBody);
public record BulkEmailRequest(
    string Subject,
    string HtmlBody,
    string? Country = null,
    string? Stage = null,
    string? LeadStatus = null,
    Guid? AssignedToUserId = null);

// --- Commands ---
public record SendEmailCommand(SendEmailRequest Request) : IRequest;
public record SendBulkEmailCommand(BulkEmailRequest Request) : IRequest<int>;

// --- Handlers ---
public class SendEmailHandler(IEmailService emailService) : IRequestHandler<SendEmailCommand>
{
    public async Task Handle(SendEmailCommand cmd, CancellationToken ct) =>
        await emailService.SendAsync(
            cmd.Request.ToEmail, cmd.Request.ToName,
            cmd.Request.Subject, cmd.Request.HtmlBody);
}

public class SendBulkEmailHandler(IAppDbContext context, IEmailService emailService)
    : IRequestHandler<SendBulkEmailCommand, int>
{
    public async Task<int> Handle(SendBulkEmailCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        var query = context.Companies.Where(c => c.IsActive && c.Email != null);

        if (!string.IsNullOrWhiteSpace(r.Country))
            query = query.Where(c => c.Country == r.Country);

        if (!string.IsNullOrWhiteSpace(r.Stage) &&
            Enum.TryParse<Domain.Enums.Stage>(r.Stage, true, out var stage))
            query = query.Where(c => c.Stage == stage);

        if (!string.IsNullOrWhiteSpace(r.LeadStatus))
            query = query.Where(c => c.LeadStatus == r.LeadStatus);

        if (r.AssignedToUserId.HasValue)
            query = query.Where(c => c.AssignedToUserId == r.AssignedToUserId.Value);

        var companies = await query
            .Select(c => new { c.Email, c.EnglishName })
            .ToListAsync(ct);

        var recipients = companies
            .Where(c => !string.IsNullOrWhiteSpace(c.Email))
            .Select(c => (c.Email!, c.EnglishName));

        await emailService.SendBulkAsync(recipients, r.Subject, r.HtmlBody);
        return companies.Count;
    }
}
