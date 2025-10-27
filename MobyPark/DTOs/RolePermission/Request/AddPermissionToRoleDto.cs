using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.RolePermission.Request;

public class AddPermissionToRoleDto
{
    [Required]
    [Range(1, long.MaxValue)]
    public long PermissionId { get; set; }
}
