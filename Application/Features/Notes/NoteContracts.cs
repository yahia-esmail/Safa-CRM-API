using MediatR;

namespace Application.Features.Notes;

public record NoteDto(
    Guid Id,
    Guid CompanyId,
    Guid CreatedByUserId,
    string CreatedByName,
    string Content,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateNoteRequest(string Content);

public record UpdateNoteRequest(string Content);

public record GetCompanyNotesQuery(Guid CompanyId, Guid CurrentUserId, bool IsAdmin)
    : IRequest<IEnumerable<NoteDto>>;

public record CreateNoteCommand(Guid CompanyId, CreateNoteRequest Request, Guid CurrentUserId, bool IsAdmin)
    : IRequest<NoteDto>;

public record UpdateNoteCommand(Guid NoteId, UpdateNoteRequest Request, Guid CurrentUserId, bool IsAdmin)
    : IRequest<NoteDto>;

public record DeleteNoteCommand(Guid NoteId, Guid CurrentUserId, bool IsAdmin)
    : IRequest;
