using MobyPark.DTOs.Permission.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Permission;
using MobyPark.Services.Results.RolePermission;
using MobyPark.Validation;

namespace MobyPark.Services;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissions;
    private readonly IRolePermissionService _rolePermissionService;

    public PermissionService(IPermissionRepository permissions, IRolePermissionService rPermissions)
    {
        _permissions = permissions;
        _rolePermissionService = rPermissions;
    }

    public async Task<CreatePermissionResult> CreatePermission(CreatePermissionDto dto)
    {
        dto.Resource = dto.Resource.Upper();
        dto.Action = dto.Action.Upper();

        var existsResult = await PermissionExists("resource+action", $"{dto.Resource}:{dto.Action}");
        if (existsResult is PermissionExistsResult.Exists)
            return new CreatePermissionResult.AlreadyExists();
        if (existsResult is PermissionExistsResult.InvalidInput invalid)
            return new CreatePermissionResult.Error($"Existence check failed: {invalid.Message}");
        if (existsResult is PermissionExistsResult.Error err)
            return new CreatePermissionResult.Error($"Existence check failed: {err.Message}");

        try
        {
            var permission = new PermissionModel
            {
                Resource = dto.Resource,
                Action = dto.Action
            };

            if (!await _permissions.Create(permission))
                return new CreatePermissionResult.Error("Database insertion failed.");

            return new CreatePermissionResult.Success(permission);
        }
        catch (Exception ex)
        { return new CreatePermissionResult.Error(ex.Message); }
    }

    public async Task<GetPermissionResult> GetPermissionById(long id)
    {
        var permission = await _permissions.GetById<PermissionModel>(id);
        if (permission is null)
            return new GetPermissionResult.NotFound();
        return new GetPermissionResult.Success(permission);
    }

    public async Task<GetPermissionResult> GetPermissionByResourceAndAction(string resource, string action)
    {
        if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(action))
            return new GetPermissionResult.InvalidInput("Resource and Action cannot be empty.");

        resource = resource.Upper();
        action = action.Upper();

        var permission = await _permissions.GetByResourceAndAction(resource, action);
        if (permission is null)
            return new GetPermissionResult.NotFound();

        return new GetPermissionResult.Success(permission);
    }

    public async Task<GetPermissionListResult> GetAllPermissions()
    {
        var permissions = await _permissions.GetAll();
        if (permissions.Count == 0)
            return new GetPermissionListResult.NotFound();
        return new GetPermissionListResult.Success(permissions);
    }

    public async Task<GetPermissionListResult> GetPermissionsByRoleId(long roleId)
    {
        try
        {
             var permissions = await _permissions.GetByRoleId(roleId);
            if (permissions.Count == 0)
                return new GetPermissionListResult.NotFound();
            return new GetPermissionListResult.Success(permissions);
        }
        catch (Exception ex)
        { return new GetPermissionListResult.Error(ex.Message); }
    }

    public async Task<PermissionExistsResult> PermissionExists(string checkBy, string value)
    {
        string normalizedCheckBy = checkBy.Lower();
        string trimmedValue = value.TrimSafe();

        if (string.IsNullOrEmpty(trimmedValue))
            return new PermissionExistsResult.InvalidInput("Value cannot be empty or whitespace.");

        bool exists;
        try
        {
            switch (normalizedCheckBy)
            {
                case "id":
                    if (!long.TryParse(trimmedValue, out long id))
                        return new PermissionExistsResult.InvalidInput("ID must be a valid long integer.");
                    exists = await _permissions.Exists(p => p.Id == id);
                    break;
                case "resource+action":
                    var parts = trimmedValue.Split(':');
                    if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                        return new PermissionExistsResult.InvalidInput("Value must be in 'Resource:Action' format.");
                    string resource = parts[0].Upper();
                    string action = parts[1].Upper();
                    exists = await _permissions.Exists(permission => permission.Resource == resource && permission.Action == action);
                    break;
                default:
                    return new PermissionExistsResult.InvalidInput("Invalid checkBy parameter. Must be 'id' or 'resource+action'.");
            }
        }
        catch (Exception ex)
        { return new PermissionExistsResult.Error(ex.Message); }

        return exists ? new PermissionExistsResult.Exists() : new PermissionExistsResult.NotExists();
    }

    public async Task<DeletePermissionResult> DeletePermission(long id)
    {
        var getResult = await GetPermissionById(id);
        if (getResult is not GetPermissionResult.Success success)
        {
            return getResult switch {
                GetPermissionResult.NotFound => new DeletePermissionResult.NotFound(),
                GetPermissionResult.InvalidInput invalid => new DeletePermissionResult.Error(invalid.Message),
                _ => new DeletePermissionResult.Error("Failed to check permission existence.")
            };
        }
        var permissionToDelete = success.Permission;

        var roleLinks = await _rolePermissionService.GetRolesByPermissionId(id);
        if (roleLinks is GetRolePermissionListResult.Success)
             return new DeletePermissionResult.Conflict("Cannot delete permission: It is assigned to one or more roles.");

        try
        {
            if (!await _permissions.Delete(permissionToDelete))
                return new DeletePermissionResult.Error("Database deletion failed.");

            return new DeletePermissionResult.Success();
        }
        catch (Exception ex)
        { return new DeletePermissionResult.Error(ex.Message); }
    }
}
