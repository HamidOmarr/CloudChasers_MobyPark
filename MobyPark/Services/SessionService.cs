using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using MobyPark.Models;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Session;

namespace MobyPark.Services;

public class SessionService : ISessionService
{
    private readonly IConfiguration _config;

    public SessionService(IConfiguration config)
    {
        _config = config;
    }

    public CreateJwtResult CreateSession(UserModel user)
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
}