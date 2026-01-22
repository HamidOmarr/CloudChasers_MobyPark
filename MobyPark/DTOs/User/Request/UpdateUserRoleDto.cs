using System.ComponentModel.DataAnnotations;

using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.User.Request;

[SwaggerSchema(Description = "Data for changing a user's role.")]
public class UpdateUserRoleDto : ICanBeEdited
{
    [Range(1, UserModel.DefaultUserRoleId)]
    [SwaggerSchema("The new role ID to assign to the user.")]
    public long RoleId { get; set; }
}