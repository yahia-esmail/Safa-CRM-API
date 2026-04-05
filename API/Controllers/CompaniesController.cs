using Application.Features.Activities;
using Application.Features.Companies;
using Application.Features.Contacts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class CompaniesController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? name,   [FromQuery] string? country,
        [FromQuery] int? safaKey,   [FromQuery] string? email,
        [FromQuery] string? phone,  [FromQuery] string? accountType,
        [FromQuery] string? stage,  [FromQuery] string? leadStatus,
        [FromQuery] Guid? assignedTo,
        [FromQuery] int page = 1,   [FromQuery] int size = 20)
    {
        var result = await Mediator.Send(new GetCompaniesQuery(
            name, country, safaKey, email, phone, accountType,
            stage, leadStatus, assignedTo, CurrentUserId, IsAdmin, page, size));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await Mediator.Send(new GetCompanyByIdQuery(id, CurrentUserId, IsAdmin));
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request)
    {
        var result = await Mediator.Send(new CreateCompanyCommand(request, CurrentUserId, IsAdmin));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest request)
    {
        try
        {
            var result = await Mediator.Send(new UpdateCompanyCommand(id, request, CurrentUserId, IsAdmin));
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await Mediator.Send(new DeleteCompanyCommand(id, CurrentUserId, IsAdmin));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignCompanyRequest request)
    {
        try
        {
            await Mediator.Send(new AssignCompanyCommand(id, request.AssignedToUserId));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // --- Contacts ---
    [HttpGet("{id:guid}/contacts")]
    public async Task<IActionResult> GetContacts(Guid id) =>
        Ok(await Mediator.Send(new GetContactsQuery(id)));

    [HttpPost("{id:guid}/contacts")]
    public async Task<IActionResult> AddContact(Guid id, [FromBody] CreateContactRequest request)
    {
        var req = request with { CompanyId = id };
        var result = await Mediator.Send(new CreateContactCommand(req));
        return Ok(result);
    }

    [HttpPut("{companyId:guid}/contacts/{contactId:guid}")]
    public async Task<IActionResult> UpdateContact(Guid contactId, [FromBody] UpdateContactRequest request)
    {
        try
        {
            var result = await Mediator.Send(new UpdateContactCommand(contactId, request));
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{companyId:guid}/contacts/{contactId:guid}")]
    public async Task<IActionResult> DeleteContact(Guid contactId)
    {
        try
        {
            await Mediator.Send(new DeleteContactCommand(contactId));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // --- Activities ---
    [HttpGet("{id:guid}/activities")]
    public async Task<IActionResult> GetActivities(Guid id)
    {
        try
        {
            var result = await Mediator.Send(new GetCompanyActivitiesQuery(id, CurrentUserId, IsAdmin));
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{id:guid}/activities")]
    public async Task<IActionResult> AddActivity(Guid id, [FromBody] CreateActivityRequest request)
    {
        try
        {
            var req = request with { CompanyId = id };
            var result = await Mediator.Send(new CreateActivityCommand(req, CurrentUserId, IsAdmin));
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
