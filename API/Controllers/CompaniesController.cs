using Application.Features.Activities;
using Application.Features.Companies;
using Application.Features.Contacts;
using Application.Features.Notes;
using Application.Features.Tags;
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
        [FromQuery] Guid? tagId,
        [FromQuery] int page = 1,   [FromQuery] int size = 20)
    {
        var result = await Mediator.Send(new GetCompaniesQuery(
            name, country, safaKey, email, phone, accountType,
            stage, leadStatus, assignedTo, tagId, CurrentUserId, IsAdmin, page, size));
        return Ok(result);
    }
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? name,   [FromQuery] string? country,
        [FromQuery] int? safaKey,   [FromQuery] string? email,
        [FromQuery] string? phone,  [FromQuery] string? accountType,
        [FromQuery] string? stage,  [FromQuery] string? leadStatus,
        [FromQuery] Guid? assignedTo,
        [FromQuery] Guid? tagId)
    {
        var result = await Mediator.Send(new GetCompaniesExportQuery(
            name, country, safaKey, email, phone, accountType,
            stage, leadStatus, assignedTo, tagId, CurrentUserId, IsAdmin));
        return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"companies-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
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
        try
        {
            var result = await Mediator.Send(new CreateCompanyCommand(request, CurrentUserId, IsAdmin));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
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

    [HttpPut("{companyId:guid}/activities/{activityId:guid}")]
    public async Task<IActionResult> UpdateActivity(Guid companyId, Guid activityId, [FromBody] UpdateActivityRequest request)
    {
        try
        {
            var result = await Mediator.Send(new UpdateActivityCommand(activityId, request, CurrentUserId, IsAdmin));
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPatch("{companyId:guid}/activities/{activityId:guid}/toggle-complete")]
    public async Task<IActionResult> ToggleActivityCompletion(Guid companyId, Guid activityId, [FromQuery] bool isCompleted)
    {
        try
        {
            var result = await Mediator.Send(new ToggleActivityCompletionCommand(activityId, isCompleted, CurrentUserId, IsAdmin));
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("{companyId:guid}/activities/{activityId:guid}")]
    public async Task<IActionResult> DeleteActivity(Guid companyId, Guid activityId)
    {
        try
        {
            await Mediator.Send(new DeleteActivityCommand(activityId, CurrentUserId, IsAdmin));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("{id:guid}/stage-history")]
    public async Task<IActionResult> GetStageHistory(Guid id)
    {
        try
        {
            var result = await Mediator.Send(new GetCompanyStageHistoryQuery(id, CurrentUserId, IsAdmin));
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{id:guid}/tags/{tagId:guid}")]
    public async Task<IActionResult> AssignTag(Guid id, Guid tagId)
    {
        try
        {
            await Mediator.Send(new AssignTagCommand(id, tagId, CurrentUserId, IsAdmin));
            return Ok();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("{id:guid}/tags/{tagId:guid}")]
    public async Task<IActionResult> RemoveTag(Guid id, Guid tagId)
    {
        try
        {
            await Mediator.Send(new RemoveTagCommand(id, tagId, CurrentUserId, IsAdmin));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("{id:guid}/notes")]
    public async Task<IActionResult> GetNotes(Guid id)
    {
        try
        {
            var result = await Mediator.Send(new GetCompanyNotesQuery(id, CurrentUserId, IsAdmin));
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> CreateNote(Guid id, [FromBody] CreateNoteRequest request)
    {
        try
        {
            var result = await Mediator.Send(new CreateNoteCommand(id, request, CurrentUserId, IsAdmin));
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("{companyId:guid}/notes/{noteId:guid}")]
    public async Task<IActionResult> UpdateNote(Guid companyId, Guid noteId, [FromBody] UpdateNoteRequest request)
    {
        try
        {
            var result = await Mediator.Send(new UpdateNoteCommand(noteId, request, CurrentUserId, IsAdmin));
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("{companyId:guid}/notes/{noteId:guid}")]
    public async Task<IActionResult> DeleteNote(Guid companyId, Guid noteId)
    {
        try
        {
            await Mediator.Send(new DeleteNoteCommand(noteId, CurrentUserId, IsAdmin));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("bulk-assign")]
    public async Task<IActionResult> BulkAssign([FromBody] BulkAssignRequest request)
    {
        try
        {
            await Mediator.Send(new BulkAssignCommand(request, CurrentUserId, IsAdmin));
            return Ok();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("bulk-stage")]
    public async Task<IActionResult> BulkStage([FromBody] BulkStageRequest request)
    {
        try
        {
            await Mediator.Send(new BulkStageCommand(request, CurrentUserId, IsAdmin));
            return Ok();
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
    {
        try
        {
            await Mediator.Send(new BulkDeleteCommand(request, CurrentUserId, IsAdmin));
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
