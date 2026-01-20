using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.User.Response;

[SwaggerSchema(Description = "Authentication success response containing the JWT.")]
public class AuthDto
{
    [SwaggerSchema("The unique identifier of the authenticated user.")]
    public long UserId { get; set; }
    [SwaggerSchema("The username of the authenticated user.")]
    public string Username { get; set; } = string.Empty;
    [SwaggerSchema("The email of the authenticated user.")]
    public string Email { get; set; } = string.Empty;
    [SwaggerSchema("The JWT Access Token")]
    public string Token { get; set; } = string.Empty;
    [SwaggerSchema("The JWT Refresh Token")]
    public string RefreshToken { get; set; } = string.Empty;
}