namespace MobyPark.Services.Results.RolePermission;

public abstract record RoleHasPermissionResult
{
    public sealed record HasPermission(bool Permission) : RoleHasPermissionResult;
    public sealed record NoPermission(bool Permission) : RoleHasPermissionResult;
    public sealed record Error(string Message) : RoleHasPermissionResult;
}