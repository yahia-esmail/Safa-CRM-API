using MediatR;

namespace Application.Features.Activities;

// --- DTOs ---
public record ActivityDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Type,
    string Note,
    Guid CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAt);

public record CreateActivityRequest(
    Guid CompanyId,
    string Type,
    string Note);

// --- Commands ---
public record CreateActivityCommand(CreateActivityRequest Request, Guid CurrentUserId, bool IsAdmin)
    : IRequest<ActivityDto>;

// --- Queries ---
public record GetCompanyActivitiesQuery(Guid CompanyId, Guid CurrentUserId, bool IsAdmin)
    : IRequest<IEnumerable<ActivityDto>>;
