using MobyPark.Models;

namespace MobyPark.Services.Results.Permission;

public abstract record GetPermissionResult
{
    public sealed record Success(PermissionModel Permission) : GetPermissionResult;
    public sealed record NotFound : GetPermissionResult;
    public sealed record InvalidInput(string Message) : GetPermissionResult;
}