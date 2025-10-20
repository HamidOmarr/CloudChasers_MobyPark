using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Services;

public class PermissionService
{
    private readonly IPermissionRepository _permissions;

    public PermissionService(IPermissionRepository permissions)
    {
        _permissions = permissions;
    }

    public async Task<bool> AddPermission(PermissionModel permission)
    {
        permission.Resource = permission.Resource.ToUpperInvariant();
        permission.Action = permission.Action.ToUpperInvariant();

        var existingPermission = await _permissions.GetByResourceAndAction(permission.Resource, permission.Action);
        if (existingPermission is not null) return false;

        bool createdSuccessfully = await _permissions.Create(permission);
        return createdSuccessfully;
    }

    public async Task<PermissionModel?> GetPermissionByResourceAndAction(string resource, string action)
    {
        resource = resource.ToUpperInvariant();
        action = action.ToUpperInvariant();

        var permission = await _permissions.GetByResourceAndAction(resource, action);

        return permission ?? throw new KeyNotFoundException("Permission not found");
    }

    public async Task<List<PermissionModel>> GetPermissionsByRoleId(long roleId)
    {
        var permissions = await _permissions.GetByRoleId(roleId);
        if (permissions.Count == 0)
            throw new KeyNotFoundException("No permissions found for the given role ID");

        return permissions;
    }
}