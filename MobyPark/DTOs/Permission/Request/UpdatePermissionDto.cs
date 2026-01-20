using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Permission.Request;

[SwaggerSchema(Description = "Data required to update an existing permission.")]
public class UpdatePermissionDto
{
    [Required]
    [StringLength(100)]
    [SwaggerSchema("The new resource identifier.")]
    public string Resource { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [SwaggerSchema("The new action identifier.")]
    public string Action { get; set; } = string.Empty;
}