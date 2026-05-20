using MediatR;

namespace Application.Features.Tags;

public record TagDto(Guid Id, string Name, string Color, Guid CreatedByUserId, string CreatedByName);

public record CreateTagRequest(string Name, string Color);

public record GetTagsQuery() : IRequest<IEnumerable<TagDto>>;

public record CreateTagCommand(CreateTagRequest Request, Guid CreatedByUserId) : IRequest<TagDto>;

public record DeleteTagCommand(Guid Id, bool IsAdmin) : IRequest;

public record AssignTagCommand(Guid CompanyId, Guid TagId, Guid CurrentUserId, bool IsAdmin) : IRequest;

public record RemoveTagCommand(Guid CompanyId, Guid TagId, Guid CurrentUserId, bool IsAdmin) : IRequest;
