using Application.Features.Activities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class ActivitiesController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks([FromQuery] bool? isCompleted)
    {
        var result = await Mediator.Send(new GetMyTasksQuery(isCompleted, CurrentUserId, IsAdmin));
        return Ok(result);
    }
}
