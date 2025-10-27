using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Permission;
using MobyPark.Services.Results.Role;
using MobyPark.Services.Results.RolePermission;

namespace MobyPark.Services;

public class RolePermissionService : IRolePermissionService
{
    private readonly IRolePermissionRepository _rolePermissions;
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;

    public RolePermissionService(IRolePermissionRepository rPermission, IRoleService roleService, IPermissionService permissionService)
    {
        _rolePermissions = rPermission;
        _roleService = roleService;
        _permissionService = permissionService;
    }

    private async Task<RoleHasPermissionResult> RoleHasPermission(long roleId, long permissionId)
    {
        try
        {
            bool hasPermission = await _rolePermissions.RoleHasPermission(roleId, permissionId);
            if (hasPermission)
                return new RoleHasPermissionResult.HasPermission(true);
            return new RoleHasPermissionResult.NoPermission(false);
        }
        catch (Exception ex)
        { return new RoleHasPermissionResult.Error(ex.Message); }
    }

    public async Task<GetRolePermissionListResult> GetRolePermissionsByRoleId(long roleId)
    {
        try
        {
            var permissions = await _rolePermissions.GetPermissionsByRoleId(roleId);
            if (permissions.Count == 0)
                return new GetRolePermissionListResult.NotFound();
            return new GetRolePermissionListResult.Success(permissions);
        }
        catch (Exception ex)
        { return new GetRolePermissionListResult.Error(ex.Message); }
    }

    public async Task<GetRolePermissionListResult> GetRolesByPermissionId(long permissionId)
    {
        try
        {
            var roles = await _rolePermissions.GetRolesByPermissionId(permissionId);
            if (roles.Count == 0)
                return new GetRolePermissionListResult.NotFound();
            return new GetRolePermissionListResult.Success(roles);
        }
        catch (Exception ex)
        { return new GetRolePermissionListResult.Error(ex.Message); }
    }

    public async Task<AddPermissionToRoleResult> AddPermissionToRole(long roleId, long permissionId)
    {
        var roleExists = await _roleService.RoleExists("id", roleId.ToString());
        if (roleExists is not RoleExistsResult.Exists)
            return roleExists switch
            {
                RoleExistsResult.NotExists => new AddPermissionToRoleResult.RoleNotFound(),
                RoleExistsResult.Error roleErr => new AddPermissionToRoleResult.Error($"Role check failed: {roleErr.Message}"),
                _ => new AddPermissionToRoleResult.Error("Unknown error during role existence check.")
            };

        var permExists = await _permissionService.PermissionExists("id", permissionId.ToString());
        if (permExists is not PermissionExistsResult.Exists)
            return permExists switch
            {
                PermissionExistsResult.NotExists => new AddPermissionToRoleResult.PermissionNotFound(),
                PermissionExistsResult.Error permErr => new AddPermissionToRoleResult.Error($"Permission check failed: {permErr.Message}"),
                _ => new AddPermissionToRoleResult.Error("Unknown error during permission existence check.")
            };

        var alreadyHasPermissionResult = await RoleHasPermission(roleId, permissionId);

        if (alreadyHasPermissionResult is RoleHasPermissionResult.HasPermission)
            return new AddPermissionToRoleResult.AlreadyAssigned();

        if (alreadyHasPermissionResult is RoleHasPermissionResult.Error hasPermErr)
            return new AddPermissionToRoleResult.Error($"Error checking existing permission: {hasPermErr.Message}");

        try
        {
            var rolePermission = new RolePermissionModel { RoleId = roleId, PermissionId = permissionId };

            try
            {
                if (roleId != UserModel.AdminRoleId && alreadyHasPermissionResult is RoleHasPermissionResult.NoPermission)
                {
                    var adminRolePermission = new RolePermissionModel { RoleId = 1, PermissionId = permissionId };
                    if (!await _rolePermissions.AddPermissionToRole(adminRolePermission))
                    {
                        return new AddPermissionToRoleResult.Error(
                            "Failed to automatically assign permission to ADMIN role.");
                    }
                }

                if (!await _rolePermissions.AddPermissionToRole(rolePermission))
                    return new AddPermissionToRoleResult.Error("Database operation failed to add permission.");

                return new AddPermissionToRoleResult.Success(rolePermission);
            }
            catch (Exception ex)
            { return new AddPermissionToRoleResult.Error(ex.Message); }
        }
        catch (Exception ex)
        { return new AddPermissionToRoleResult.Error(ex.Message); }
    }

    public async Task<RemovePermissionFromRoleResult> RemovePermissionFromRole(long roleId, long permissionId)
    {
        if (roleId == UserModel.AdminRoleId)
            return new RemovePermissionFromRoleResult.Forbidden("Cannot remove permissions from the ADMIN role.");

        var rolePermissionResult = await RoleHasPermission(roleId, permissionId);

        if (rolePermissionResult is RoleHasPermissionResult.NoPermission)
            return new RemovePermissionFromRoleResult.NotFound();

        var rolePermission = new RolePermissionModel { RoleId = roleId, PermissionId = permissionId };

        try
        {
            if (!await _rolePermissions.RemovePermissionFromRole(rolePermission))
                return new RemovePermissionFromRoleResult.Error("Database operation failed to remove permission.");

            return new RemovePermissionFromRoleResult.Success();
        }
        catch (Exception ex)
        { return new RemovePermissionFromRoleResult.Error(ex.Message); }
    }
}
