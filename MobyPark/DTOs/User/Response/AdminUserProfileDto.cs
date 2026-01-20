using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.User.Response;

[SwaggerSchema(Description = "Extended user profile information for admins.")]
public class AdminUserProfileDto : UserProfileDto
{
    [SwaggerSchema("The name of the user's role")]
    public string Role { get; set; } = "USER";
    [SwaggerSchema("The date and time when the user was created")]
    public DateTimeOffset CreatedAt { get; set; }
}