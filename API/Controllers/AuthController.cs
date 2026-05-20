using Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var result = await mediator.Send(new LoginCommand(request.Email, request.Password, ipAddress));
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var result = await mediator.Send(new RefreshTokenCommand(request.RefreshToken, ipAddress));
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")!);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        await mediator.Send(new LogoutCommand(userId, ipAddress));
        return NoContent();
    }

    // ── New Endpoints ───────────────────────────────────────────────

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        return Ok(await mediator.Send(new GetMeQuery()));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var result = await mediator.Send(new RegisterCommand(request, ipAddress));
            return Ok(result);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await mediator.Send(new ForgotPasswordCommand(request.Email));
        return NoContent(); // Always return 204 to prevent enumeration
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await mediator.Send(new ResetPasswordCommand(request));
            return Ok(new { message = "Password has been successfully reset." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
