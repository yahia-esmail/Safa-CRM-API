using Application.Features.SmtpSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Epic A — Per-User SMTP Settings.
/// Allows users to register and manage their own SMTP server configuration.
/// </summary>
[ApiController]
[Route("api/smtp")]
[Authorize]
public class SmtpController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/smtp — Get own SMTP settings (excluding password).</summary>
    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            var result = await mediator.Send(new GetSmtpQuery());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>POST /api/smtp — Add SMTP settings (one per user).</summary>
    [HttpPost]
    public async Task<IActionResult> CreateSettings([FromBody] CreateOrUpdateSmtpRequest request)
    {
        try
        {
            var result = await mediator.Send(new CreateOrUpdateSmtpCommand(request));
            return CreatedAtAction(nameof(GetSettings), result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PUT /api/smtp — Update SMTP settings.</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] CreateOrUpdateSmtpRequest request)
    {
        try
        {
            var result = await mediator.Send(new CreateOrUpdateSmtpCommand(request));
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>DELETE /api/smtp — Remove SMTP settings.</summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteSettings()
    {
        try
        {
            await mediator.Send(new DeleteSmtpCommand());
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>POST /api/smtp/test — Send test email to self to verify config.</summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestSettings([FromBody] TestSmtpRequest request)
    {
        try
        {
            var result = await mediator.Send(new TestSmtpCommand(request));
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
