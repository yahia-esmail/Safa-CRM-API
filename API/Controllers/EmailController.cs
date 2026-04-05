using Application.Features.Email;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class EmailController(IMediator mediator) : BaseController(mediator)
{
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendEmailRequest request)
    {
        try
        {
            await Mediator.Send(new SendEmailCommand(request));
            return Ok(new { message = "Email sent successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to send email: {ex.Message}" });
        }
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> Bulk([FromBody] BulkEmailRequest request)
    {
        try
        {
            var count = await Mediator.Send(new SendBulkEmailCommand(request));
            return Ok(new { message = $"Bulk email sent to {count} companies." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to send bulk email: {ex.Message}" });
        }
    }
}
