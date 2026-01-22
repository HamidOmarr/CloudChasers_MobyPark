using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Role.Response;

[SwaggerSchema(Description = "Result of a role existence check.")]
public class RoleExistsResponseDto
{
    [SwaggerSchema("True if the role exists, false otherwise.")]
    public bool Exists { get; set; }
}