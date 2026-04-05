using Application.Common;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Contacts;

// --- DTOs ---
public record ContactDto(
    Guid Id, Guid CompanyId, string? Name, string? Email,
    string? Phone, string? JobTitle, DateTime CreatedAt);

public record CreateContactRequest(
    Guid CompanyId, string? Name, string? Email,
    string? Phone, string? JobTitle);

public record UpdateContactRequest(
    string? Name, string? Email, string? Phone, string? JobTitle);

// --- Commands & Queries ---
public record GetContactsQuery(Guid CompanyId) : IRequest<IEnumerable<ContactDto>>;
public record CreateContactCommand(CreateContactRequest Request) : IRequest<ContactDto>;
public record UpdateContactCommand(Guid Id, UpdateContactRequest Request) : IRequest<ContactDto>;
public record DeleteContactCommand(Guid Id) : IRequest;

// --- Handlers ---
public class GetContactsHandler(IAppDbContext context)
    : IRequestHandler<GetContactsQuery, IEnumerable<ContactDto>>
{
    public async Task<IEnumerable<ContactDto>> Handle(GetContactsQuery q, CancellationToken ct) =>
        await context.CompanyContacts
            .Where(c => c.CompanyId == q.CompanyId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ContactDto(c.Id, c.CompanyId, c.Name, c.Email, c.Phone, c.JobTitle, c.CreatedAt))
            .ToListAsync(ct);
}

public class CreateContactHandler(IAppDbContext context)
    : IRequestHandler<CreateContactCommand, ContactDto>
{
    public async Task<ContactDto> Handle(CreateContactCommand cmd, CancellationToken ct)
    {
        var contact = new CompanyContact
        {
            CompanyId = cmd.Request.CompanyId,
            Name = cmd.Request.Name,
            Email = cmd.Request.Email,
            Phone = PhoneHelper.ToE164(cmd.Request.Phone) ?? cmd.Request.Phone,
            JobTitle = cmd.Request.JobTitle
        };
        context.CompanyContacts.Add(contact);
        await context.SaveChangesAsync(ct);
        return new ContactDto(contact.Id, contact.CompanyId, contact.Name, contact.Email, contact.Phone, contact.JobTitle, contact.CreatedAt);
    }
}

public class UpdateContactHandler(IAppDbContext context)
    : IRequestHandler<UpdateContactCommand, ContactDto>
{
    public async Task<ContactDto> Handle(UpdateContactCommand cmd, CancellationToken ct)
    {
        var contact = await context.CompanyContacts.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException("Contact not found.");
        contact.Name = cmd.Request.Name;
        contact.Email = cmd.Request.Email;
        contact.Phone = PhoneHelper.ToE164(cmd.Request.Phone) ?? cmd.Request.Phone;
        contact.JobTitle = cmd.Request.JobTitle;
        await context.SaveChangesAsync(ct);
        return new ContactDto(contact.Id, contact.CompanyId, contact.Name, contact.Email, contact.Phone, contact.JobTitle, contact.CreatedAt);
    }
}

public class DeleteContactHandler(IAppDbContext context)
    : IRequestHandler<DeleteContactCommand>
{
    public async Task Handle(DeleteContactCommand cmd, CancellationToken ct)
    {
        var contact = await context.CompanyContacts.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException("Contact not found.");
        context.CompanyContacts.Remove(contact);
        await context.SaveChangesAsync(ct);
    }
}
