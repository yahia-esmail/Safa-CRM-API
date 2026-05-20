using Application.Features.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// B-3 — In-App Notifications API Endpoints.
/// Pull-based: frontend polls unread-count every 30 seconds.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// GET /api/notifications — List notifications (paginated + read state / type filter).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? isRead,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20)
    {
        var result = await mediator.Send(new GetNotificationsQuery(isRead, type, page, size));
        return Ok(result);
    }

    /// <summary>
    /// GET /api/notifications/unread-count — Get the number of unread notifications for current user.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await mediator.Send(new GetUnreadCountQuery());
        return Ok(new { count });
    }

    /// <summary>
    /// PUT /api/notifications/{id}/read — Mark a specific notification as read.
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        try
        {
            await mediator.Send(new MarkNotificationReadCommand(id));
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/notifications/read-all — Mark all notifications for current user as read.
    /// </summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await mediator.Send(new MarkAllNotificationsReadCommand());
        return NoContent();
    }

    /// <summary>
    /// DELETE /api/notifications/{id} — Delete a specific notification.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        try
        {
            await mediator.Send(new DeleteNotificationCommand(id));
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
