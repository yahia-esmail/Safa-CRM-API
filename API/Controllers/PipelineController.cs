using Application.Features.Companies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[Route("api/pipeline")]
public class PipelineController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetSummary()
    {
        var result = await Mediator.Send(new GetPipelineSummaryQuery(CurrentUserId, IsAdmin));
        return Ok(result);
    }
}
