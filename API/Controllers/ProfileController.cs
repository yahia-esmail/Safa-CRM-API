using Application.Common.Interfaces;
using Application.Features.Tenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Profile management for the currently logged-in user (any role).
/// Routes: /api/profile
/// </summary>
[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController(IMediator mediator, IFileUploadService fileUploadService, IAppDbContext dbContext) : ControllerBase
{
    private readonly string[] _allowedAvatarExtensions = { ".jpg", ".jpeg", ".png" };
    private const long MaxAvatarSize = 2 * 1024 * 1024; // 2 MB

    /// <summary>GET /api/profile — Get current user's profile.</summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        try { return Ok(await mediator.Send(new GetMyProfileQuery())); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>PUT /api/profile — Update name, phone, email, and avatar.</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        try
        {
            return Ok(await mediator.Send(new UpdateMyProfileCommand(req.Name, req.Email, req.Phone, req.AvatarUrl)));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>PUT /api/profile/password — Change own password.</summary>
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        try
        {
            await mediator.Send(new ChangeMyPasswordCommand(req.CurrentPassword, req.NewPassword, req.ConfirmPassword));
            return Ok(new { message = "Password changed successfully." });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>POST /api/profile/avatar — Upload profile picture (max 2MB, .jpg/.png only).</summary>
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        if (file.Length > MaxAvatarSize)
            return BadRequest(new { message = "File size exceeds 2 MB limit." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedAvatarExtensions.Contains(extension))
            return BadRequest(new { message = "Only .jpg, .jpeg, and .png files are allowed." });

        try
        {
            // Upload to avatars folder
            var avatarUrl = await fileUploadService.UploadFileAsync(file, "avatars");

            // Update user profile record directly
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var user = await dbContext.Users.FindAsync(userId);
                if (user != null)
                {
                    // Clean up old avatar if any
                    if (!string.IsNullOrEmpty(user.AvatarUrl))
                    {
                        fileUploadService.DeleteFile(user.AvatarUrl);
                    }
                    user.AvatarUrl = avatarUrl;
                    await dbContext.SaveChangesAsync();
                }
            }

            return Ok(new { avatarUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to upload avatar: {ex.Message}" });
        }
    }
}

// ── Request DTOs ───────────────────────────────────────────────────────────────
public record UpdateProfileRequest(string Name, string Email, string? Phone, string? AvatarUrl);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

