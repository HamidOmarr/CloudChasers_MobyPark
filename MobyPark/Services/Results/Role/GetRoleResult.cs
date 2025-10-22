using MobyPark.Models;

namespace MobyPark.Services.Results.Role;

public abstract record GetRoleResult
{
    public sealed record Success(RoleModel Role) : GetRoleResult;
    public sealed record NotFound : GetRoleResult;
    public sealed record InvalidInput(string Message) : GetRoleResult;
    public sealed record Error(string Message) : GetRoleResult;
}
