using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Validation;

namespace MobyPark.Services;

public class RolePermissionService
{
    private readonly IRolePermissionRepository _rolePermissionRepository;

    public RolePermissionService(IRepositoryStack repoStack)
    {
        _rolePermissionRepository = repoStack.RolePermissions;
    }

    private async Task<bool> RoleHasPermission(long roleId, long permissionId)
    {
        ServiceValidator.RolePermission(new RolePermissionModel { RoleId = roleId, PermissionId = permissionId } );

        bool hasPermission = await _rolePermissionRepository.RoleHasPermission(roleId, permissionId);
        return hasPermission;
    }

    public async Task<List<RolePermissionModel>> GetRolePermissionsByRoleId(long roleId)
    {
        var permissions = await _rolePermissionRepository.GetPermissionsByRoleId(roleId);
        if (permissions.Count == 0)
            throw new Exception("No permissions found for the given role ID.");
        return permissions;
    }

    public async Task<List<RolePermissionModel>> GetRolesByPermissionId(long permissionId)
    {
        var roles = await _rolePermissionRepository.GetRolesByPermissionId(permissionId);
        if (roles.Count == 0)
            throw new Exception("No roles found for the given permission ID.");

        return roles;
    }

    public async Task<bool> AddPermissionToRole(long roleId, long permissionId)
    {
        var alreadyHasPermission = await RoleHasPermission(roleId, permissionId);
        if (alreadyHasPermission) return false; // ALREADY ASSIGNED. CHANGE TO CUSTOM RETURN TYPE LATER.

        if (!await RoleHasPermission(1, permissionId))
            await _rolePermissionRepository.AddPermissionToRole(
                new RolePermissionModel { RoleId = 1, PermissionId = permissionId });

        return await _rolePermissionRepository.AddPermissionToRole(new RolePermissionModel { RoleId = roleId, PermissionId = permissionId });
    }

    public async Task<bool> RemovePermissionFromRole(long roleId, long permissionId)
    {
        if (roleId == 1)
            throw new Exception("Cannot remove permissions from the admin role.");

        var hasPermission = await RoleHasPermission(roleId, permissionId);
        if (!hasPermission) return false; // DOES NOT HAVE PERMISSION. CHANGE TO CUSTOM RETURN TYPE LATER.

        return await _rolePermissionRepository.RemovePermissionFromRole(new RolePermissionModel { RoleId = roleId, PermissionId = permissionId });
    }
}
