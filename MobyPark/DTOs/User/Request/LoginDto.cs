using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.User.Request;

[SwaggerSchema(Description = "Credentials required to authenticate a user.")]
public class LoginDto
{
    [Required]
    [SwaggerSchema("Username or Email address")]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("User password")]
    public string Password { get; set; } = string.Empty;
}