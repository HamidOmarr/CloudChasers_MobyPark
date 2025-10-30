namespace MobyPark.Models.Repositories.Interfaces;

public interface IRoleRepository : IRepository<RoleModel>
{
    Task<RoleModel?> GetByName(string roleName);
    Task<bool> AnyUsersAssigned(long roleId);
    Task<bool> DeleteRole(RoleModel role);
}
