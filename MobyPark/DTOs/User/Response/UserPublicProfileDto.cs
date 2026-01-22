using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.User.Response;

[SwaggerSchema(Description = "Limited public profile information for a user.")]
public class UserPublicProfileDto
{
    [SwaggerSchema("The unique identifier of the user.")]
    public long Id { get; set; }
    [SwaggerSchema("The username of the user.")]
    public string Username { get; set; } = string.Empty;
    [SwaggerSchema("The first name of the user.")]
    public string FirstName { get; set; } = string.Empty;
    [SwaggerSchema("The last name of the user.")]
    public string LastName { get; set; } = string.Empty;
}