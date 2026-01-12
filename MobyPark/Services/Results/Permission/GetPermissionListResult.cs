using MobyPark.Models;

namespace MobyPark.Services.Results.Permission;

public abstract record GetPermissionListResult
{
    public sealed record Success(List<PermissionModel> Permissions) : GetPermissionListResult;
    public sealed record NotFound : GetPermissionListResult;
    public sealed record Error(string Message) : GetPermissionListResult;
}