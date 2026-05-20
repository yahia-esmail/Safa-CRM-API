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
    DateTime CreatedAt,
    DateTime? DueDate,
    bool IsCompleted,
    DateTime? CompletedAt);

public record CreateActivityRequest(
    Guid CompanyId,
    string Type,
    string Note,
    DateTime? DueDate = null);

public record UpdateActivityRequest(
    string Type,
    string Note,
    DateTime? DueDate = null,
    bool IsCompleted = false);

// --- Commands ---
public record CreateActivityCommand(CreateActivityRequest Request, Guid CurrentUserId, bool IsAdmin)
    : IRequest<ActivityDto>;

public record UpdateActivityCommand(Guid Id, UpdateActivityRequest Request, Guid CurrentUserId, bool IsAdmin)
    : IRequest<ActivityDto>;

public record ToggleActivityCompletionCommand(Guid Id, bool IsCompleted, Guid CurrentUserId, bool IsAdmin)
    : IRequest<ActivityDto>;

public record DeleteActivityCommand(Guid Id, Guid CurrentUserId, bool IsAdmin)
    : IRequest;

// --- Queries ---
public record GetCompanyActivitiesQuery(Guid CompanyId, Guid CurrentUserId, bool IsAdmin)
    : IRequest<IEnumerable<ActivityDto>>;

public record GetMyTasksQuery(bool? IsCompleted, Guid CurrentUserId, bool IsAdmin)
    : IRequest<IEnumerable<ActivityDto>>;


