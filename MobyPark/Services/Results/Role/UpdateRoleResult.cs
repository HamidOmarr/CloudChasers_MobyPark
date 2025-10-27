using MobyPark.Models;

namespace MobyPark.Services.Results.Role;

public abstract record UpdateRoleResult
{
    public sealed record Success(RoleModel Role) : UpdateRoleResult;
    public sealed record NotFound : UpdateRoleResult;
    public sealed record Error(string Message) : UpdateRoleResult;
    public sealed record NoChangesMade : UpdateRoleResult;
}
