using System.Security.Claims;

using MobyPark.Models;
using MobyPark.Services.Results.Tokens;

namespace MobyPark.Services.Interfaces;

public interface ITokenService
{
    CreateJwtResult CreateToken(UserModel user);
    ClaimsPrincipal? ValidateToken(string token);
    string GenerateRefreshToken();
    Task<TokenRefreshResult> RefreshToken(string refreshToken);
    DateTimeOffset GetSlidingTokenExpiryTime();
    DateTimeOffset GetAbsoluteTokenExpiryTime();
}