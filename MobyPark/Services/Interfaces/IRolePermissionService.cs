using MobyPark.Services.Results.RolePermission;

namespace MobyPark.Services.Interfaces;

public interface IRolePermissionService
{
    Task<GetRolePermissionListResult> GetRolePermissionsByRoleId(long roleId);
    Task<GetRolePermissionListResult> GetRolesByPermissionId(long permissionId);
    Task<AddPermissionToRoleResult> AddPermissionToRole(long roleId, long permissionId);
    Task<RemovePermissionFromRoleResult> RemovePermissionFromRole(long roleId, long permissionId);
}
