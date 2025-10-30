using MobyPark.Models;

namespace MobyPark.Services.Results.Role;

public abstract record GetRoleListResult
{
    public sealed record Success(List<RoleModel> Roles) : GetRoleListResult;
    public sealed record NotFound : GetRoleListResult;
}
