using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Services.Services;

namespace MobyPark.Services;

public class PermissionService
{
    private readonly IPermissionRepository _permissions;

    public PermissionService(IRepositoryStack repoStack)
    {
        _permissions = repoStack.Permissions;
    }

    public async Task<bool> AddPermission(PermissionModel permission)
    {
        Validator.Permission(permission);

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

        Validator.Permission(new PermissionModel{ Resource = resource, Action = action } );

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