using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Role.Request;

[SwaggerSchema(Description = "Data required to create a new role.")]
public class CreateRoleDto
{
    [Required]
    [MaxLength(50)]
    [SwaggerSchema("The unique name of the role (e.g., 'Manager').")]
    public string Name { get; set; } = string.Empty;

    [SwaggerSchema("A description of the role's purpose.")]
    public string Description { get; set; } = string.Empty;
}