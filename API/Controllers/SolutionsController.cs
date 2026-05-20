using Application.Features.Solutions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class SolutionsController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = true) =>
        Ok(await Mediator.Send(new GetSolutionsQuery(activeOnly)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try { return Ok(await Mediator.Send(new GetSolutionByIdQuery(id))); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSolutionRequest request)
    {
        var result = await Mediator.Send(new CreateSolutionCommand(request));
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSolutionRequest request)
    {
        try { return Ok(await Mediator.Send(new UpdateSolutionCommand(id, request))); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await Mediator.Send(new DeleteSolutionCommand(id));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
