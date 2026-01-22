using System.ComponentModel.DataAnnotations;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Permission.Request;

[SwaggerSchema(Description = "Data required to define a new system permission.")]
public class CreatePermissionDto
{
    [Required]
    [StringLength(100)]
    [SwaggerSchema("The resource associated with the permission (e.g., 'ParkingLot').")]
    public string Resource { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [SwaggerSchema("The specific action allowed on the resource (e.g., 'Create', 'Read').")]
    public string Action { get; set; } = string.Empty;
}