using System.ComponentModel.DataAnnotations;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.DTOs.User.Request;

public class UpdateUserRoleDto : ICanBeEdited
{
    [Range(1, UserModel.DefaultUserRoleId)]
    public long RoleId { get; set; }
}
