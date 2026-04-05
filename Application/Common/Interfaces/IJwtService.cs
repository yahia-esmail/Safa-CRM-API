namespace Application.Common.Interfaces;

/// <summary>Application-level JWT abstraction</summary>
public interface IJwtService
{
    string GenerateAccessToken(Domain.Entities.SystemUser user);
    string GenerateRefreshToken();
    DateTime RefreshTokenExpiry();
}
