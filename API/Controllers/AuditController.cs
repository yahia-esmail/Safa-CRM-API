using Application.Features.Audit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class AuditController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? entity,
        [FromQuery] string? entityId,
        [FromQuery] Guid? userId,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        try
        {
            var result = await Mediator.Send(new GetAuditLogsQuery(entity, entityId, userId, action, from, to, CurrentUserId, IsAdmin));
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
