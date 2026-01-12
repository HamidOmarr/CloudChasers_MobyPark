using MobyPark.DTOs.Permission.Request;
using MobyPark.Services.Results.Permission;

namespace MobyPark.Services.Interfaces;

public interface IPermissionService
{
    Task<CreatePermissionResult> CreatePermission(CreatePermissionDto dto);
    Task<GetPermissionResult> GetPermissionById(long id);
    Task<GetPermissionResult> GetPermissionByResourceAndAction(string resource, string action);
    Task<GetPermissionListResult> GetAllPermissions();
    Task<GetPermissionListResult> GetPermissionsByRoleId(long roleId);
    Task<PermissionExistsResult> PermissionExists(string checkBy, string value);
    Task<DeletePermissionResult> DeletePermission(long id);
}