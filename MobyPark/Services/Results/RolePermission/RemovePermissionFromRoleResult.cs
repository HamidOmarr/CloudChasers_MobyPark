namespace MobyPark.Services.Results.RolePermission;

public abstract record RemovePermissionFromRoleResult
{
    public sealed record Success : RemovePermissionFromRoleResult;
    public sealed record NotFound : RemovePermissionFromRoleResult;
    public sealed record Forbidden(string Message) : RemovePermissionFromRoleResult;
    public sealed record Error(string Message) : RemovePermissionFromRoleResult;
}