using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace API.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var val = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public string? Role => User?.FindFirstValue(ClaimTypes.Role) ?? User?.FindFirstValue("role");

    public Guid? TenantId
    {
        get
        {
            var val = User?.FindFirst("TenantId")?.Value;
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }
}
