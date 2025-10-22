using MobyPark.Models;

namespace MobyPark.Services.Results.RolePermission;

public abstract record AddPermissionToRoleResult
{
    public sealed record Success(RolePermissionModel RolePermission) : AddPermissionToRoleResult;
    public sealed record AlreadyAssigned : AddPermissionToRoleResult;
    public sealed record RoleNotFound : AddPermissionToRoleResult;
    public sealed record PermissionNotFound : AddPermissionToRoleResult;
    public sealed record Error(string Message) : AddPermissionToRoleResult;
}
