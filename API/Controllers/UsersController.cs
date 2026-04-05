using Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await Mediator.Send(new GetUsersQuery()));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try { return Ok(await Mediator.Send(new GetUserByIdQuery(id))); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var result = await Mediator.Send(new CreateUserCommand(request));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        try { return Ok(await Mediator.Send(new UpdateUserCommand(id, request))); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
