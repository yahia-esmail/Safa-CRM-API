using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController(IMediator mediator) : ControllerBase
{
    protected readonly IMediator Mediator = mediator;

    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User not authenticated."));

    protected bool IsAdmin =>
        User.IsInRole("Admin") || User.FindFirstValue("role") == "Admin";
}
