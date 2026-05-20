using Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class UsersController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>GET /api/users — List users (Admin sees own tenant; SuperAdmin sees all).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await Mediator.Send(new GetUsersQuery()));

    /// <summary>GET /api/users/{id} — Get a single user by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try { return Ok(await Mediator.Send(new GetUserByIdQuery(id))); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
    /// <summary>GET /api/users/{id}/companies — Get all companies assigned to a user.</summary>
    [HttpGet("{id:guid}/companies")]
    public async Task<IActionResult> GetCompanies(Guid id)
    {
        try { return Ok(await Mediator.Send(new GetUserCompaniesQuery(id))); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>POST /api/users — Create a user. SuperAdmin creates Admins; Admin creates Sales.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            var result = await Mediator.Send(new CreateUserCommand(request));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PUT /api/users/{id} — Update user details (name, email, role, active status).</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        try { return Ok(await Mediator.Send(new UpdateUserCommand(id, request))); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PATCH /api/users/{id}/toggle — Activate or deactivate a user. Deactivating Admin cascades to their Sales team.</summary>
    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleUserRequest request)
    {
        try { return Ok(await Mediator.Send(new ToggleUserStatusCommand(id, request.IsActive))); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>DELETE /api/users/{id} — Hard-delete a user (blocked if user has sales orders; use toggle instead).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await Mediator.Send(new DeleteUserCommand(id));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

// ── Request DTOs ───────────────────────────────────────────────────────────────
public record ToggleUserRequest(bool IsActive);

