using MobyPark.Models;

namespace MobyPark.Services.Results.RolePermission;

public abstract record GetRolePermissionListResult
{
    public sealed record Success(List<RolePermissionModel> RolePermissions) : GetRolePermissionListResult;
    public sealed record NotFound : GetRolePermissionListResult;
    public sealed record Error(string Message) : GetRolePermissionListResult;
}