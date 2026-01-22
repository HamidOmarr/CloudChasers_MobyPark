using System.ComponentModel.DataAnnotations;

using MobyPark.Models.Repositories.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Role.Request;

[SwaggerSchema(Description = "Data required to update an existing role.")]
public class UpdateRoleDto : ICanBeEdited
{
    [Required]
    [SwaggerSchema("The unique identifier of the role to update.")]
    public long Id { get; set; }

    [MaxLength(50)]
    [SwaggerSchema("The new name for the role (optional).")]
    public string? Name { get; set; }

    [SwaggerSchema("The new description for the role (optional).")]
    public string? Description { get; set; }
}