using System.Security.Claims;
using System.Text;
using MobyPark.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using MobyPark.DTOs.Token;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Tokens;

namespace MobyPark.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly IRepository<UserModel> _userRepo;

    public TokenService(IConfiguration config, IRepository<UserModel> userRepo)
    {
        _config = config;
        _userRepo = userRepo;
    }

    public CreateJwtResult CreateToken(UserModel user)
    {
        string? secretKey = _config["Jwt:Key"];
        string issuer = _config["Jwt:Issuer"] ?? "MobyParkAPI";
        string audience = _config["Jwt:Audience"] ?? "MobyParkUsers";

        if (string.IsNullOrEmpty(secretKey))
            return new CreateJwtResult.ConfigError("JWT secret key is not configured.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.Name),
        };

        claims.AddRange(
            from rolePermission in user.Role.RolePermissions
            where !string.IsNullOrEmpty(rolePermission.Permission.Key)
            select new Claim("Permission", rolePermission.Permission.Key!));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new CreateJwtResult.Success(new JwtSecurityTokenHandler().WriteToken(token));
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        string? secretKey = _config["Jwt:Key"];
        string? issuer = _config["Jwt:Issuer"];
        string? audience = _config["Jwt:Audience"];

        if (string.IsNullOrEmpty(secretKey))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            byte[] key = Encoding.UTF8.GetBytes(secretKey);

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),

                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            return tokenHandler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    public string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<TokenRefreshResult> RefreshToken(string refreshToken)
    {
        UserModel? user = await _userRepo.GetSingleByAsync(e => e.RefreshToken == refreshToken);

        if (user is null)
            return new TokenRefreshResult.InvalidToken("Invalid refresh token.");

        if (user.SlidingTokenExpiryTime <= DateTimeOffset.UtcNow)
            return new TokenRefreshResult.InvalidToken("Refresh token has expired due to inactivity.");

        if (user.AbsoluteTokenExpiryTime <= DateTimeOffset.UtcNow)
            return new TokenRefreshResult.InvalidToken("Session has reached maximum duration (7 days).");

        var newJwtResult = CreateToken(user);
        if (newJwtResult is not CreateJwtResult.Success success)
            return new TokenRefreshResult.Error("Failed to create new JWT token.");

        var newRefreshToken = GenerateRefreshToken();
        DateTimeOffset newSlidingExpiry = GetSlidingTokenExpiryTime();

        if (newSlidingExpiry > user.AbsoluteTokenExpiryTime)
            newSlidingExpiry = user.AbsoluteTokenExpiryTime;

        var updateData = new TokenDto {
            RefreshToken = newRefreshToken,
            SlidingTokenExpiryTime = newSlidingExpiry
        };

        await _userRepo.Update(user, updateData);

        return new TokenRefreshResult.Success(success.JwtToken, newRefreshToken);
    }

    public DateTimeOffset GetSlidingTokenExpiryTime() => DateTimeOffset.UtcNow.AddMinutes(30);
    public DateTimeOffset GetAbsoluteTokenExpiryTime() => DateTimeOffset.UtcNow.AddDays(7);
}
