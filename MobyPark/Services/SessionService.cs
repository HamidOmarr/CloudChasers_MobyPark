using System.Security.Claims;
using System.Text;
using MobyPark.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace MobyPark.Services;

public class SessionService
{
    private readonly IConfiguration _config;

    public SessionService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateSession(UserModel user)
    {
        var secretKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured.");
        var issuer = _config["Jwt:Issuer"] ?? "MobyParkAPI";
        var audience = _config["Jwt:Audience"] ?? "MobyParkUsers";

        var claims = new List<Claim>
        {
            new (ClaimTypes.NameIdentifier, user.Id.ToString()),
            new (ClaimTypes.Name, user.Username),
            new (ClaimTypes.Email, user.Email),
            new (ClaimTypes.Role, user.Role.Name)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
