using MobyPark.Models;

namespace MobyPark.Services.Results.Permission;

public abstract record CreatePermissionResult
{
    public sealed record Success(PermissionModel Permission) : CreatePermissionResult;
    public sealed record AlreadyExists : CreatePermissionResult;
    public sealed record Error(string Message) : CreatePermissionResult;
}
