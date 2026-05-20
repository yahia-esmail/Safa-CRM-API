using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Notes.Handlers;

public class GetCompanyNotesHandler(IAppDbContext context)
    : IRequestHandler<GetCompanyNotesQuery, IEnumerable<NoteDto>>
{
    public async Task<IEnumerable<NoteDto>> Handle(GetCompanyNotesQuery q, CancellationToken ct)
    {
        var company = await context.Companies.FindAsync([q.CompanyId], ct)
            ?? throw new KeyNotFoundException("Company not found.");

        if (!q.IsAdmin && company.AssignedToUserId != q.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        var notes = await context.CompanyNotes
            .Include(n => n.CreatedBy)
            .Where(n => n.CompanyId == q.CompanyId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return notes.Select(n => new NoteDto(
            n.Id,
            n.CompanyId,
            n.CreatedByUserId,
            n.CreatedBy.Name,
            n.Content,
            n.CreatedAt,
            n.UpdatedAt));
    }
}

public class CreateNoteHandler(IAppDbContext context)
    : IRequestHandler<CreateNoteCommand, NoteDto>
{
    public async Task<NoteDto> Handle(CreateNoteCommand cmd, CancellationToken ct)
    {
        var company = await context.Companies.FindAsync([cmd.CompanyId], ct)
            ?? throw new KeyNotFoundException("Company not found.");

        if (!cmd.IsAdmin && company.AssignedToUserId != cmd.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        var user = await context.Users.FindAsync([cmd.CurrentUserId], ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        var note = new CompanyNote
        {
            CompanyId = cmd.CompanyId,
            CreatedByUserId = cmd.CurrentUserId,
            Content = cmd.Request.Content,
            TenantId = company.TenantId
        };

        context.CompanyNotes.Add(note);
        await context.SaveChangesAsync(ct);

        return new NoteDto(
            note.Id,
            note.CompanyId,
            note.CreatedByUserId,
            user.Name,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt);
    }
}

public class UpdateNoteHandler(IAppDbContext context)
    : IRequestHandler<UpdateNoteCommand, NoteDto>
{
    public async Task<NoteDto> Handle(UpdateNoteCommand cmd, CancellationToken ct)
    {
        var note = await context.CompanyNotes
            .Include(n => n.CreatedBy)
            .FirstOrDefaultAsync(n => n.Id == cmd.NoteId, ct)
            ?? throw new KeyNotFoundException("Note not found.");

        // Owner only restriction (Admins can bypass)
        if (note.CreatedByUserId != cmd.CurrentUserId && !cmd.IsAdmin)
            throw new UnauthorizedAccessException("Access denied. Owner only.");

        note.Content = cmd.Request.Content;
        // note.UpdatedAt is automatically set by AuditInterceptor (D-2/D-3/D-6 rule)
        
        await context.SaveChangesAsync(ct);

        return new NoteDto(
            note.Id,
            note.CompanyId,
            note.CreatedByUserId,
            note.CreatedBy.Name,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt);
    }
}

public class DeleteNoteHandler(IAppDbContext context)
    : IRequestHandler<DeleteNoteCommand>
{
    public async Task Handle(DeleteNoteCommand cmd, CancellationToken ct)
    {
        var note = await context.CompanyNotes.FindAsync([cmd.NoteId], ct)
            ?? throw new KeyNotFoundException("Note not found.");

        // Owner only restriction (Admins can bypass)
        if (note.CreatedByUserId != cmd.CurrentUserId && !cmd.IsAdmin)
            throw new UnauthorizedAccessException("Access denied. Owner only.");

        context.CompanyNotes.Remove(note);
        await context.SaveChangesAsync(ct);
    }
}
