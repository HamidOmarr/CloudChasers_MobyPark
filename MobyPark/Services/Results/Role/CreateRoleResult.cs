using MobyPark.Models;

namespace MobyPark.Services.Results.Role;

public abstract record CreateRoleResult
{
    public sealed record Success(RoleModel Role) : CreateRoleResult;
    public sealed record AlreadyExists : CreateRoleResult;
    public sealed record Error(string Message) : CreateRoleResult;
}