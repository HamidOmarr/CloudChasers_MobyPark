using MobyPark.Models;

namespace MobyPark.Services.Results.Permission;

public abstract record UpdatePermissionResult
{
    public sealed record Success(PermissionModel Permission) : UpdatePermissionResult;
    public sealed record NotFound : UpdatePermissionResult;
    public sealed record Error(string Message) : UpdatePermissionResult;
}