using System.ComponentModel.DataAnnotations;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.RolePermission.Request;

[SwaggerSchema(Description = "Data required to assign a permission to a role.")]
public class AddPermissionToRoleDto
{
    [Required]
    [Range(1, long.MaxValue)]
    [SwaggerSchema("The unique identifier of the permission to assign.")]
    public long PermissionId { get; set; }
}