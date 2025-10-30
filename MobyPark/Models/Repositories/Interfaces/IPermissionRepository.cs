namespace MobyPark.Models.Repositories.Interfaces;

public interface IPermissionRepository : IRepository<PermissionModel>
{
    Task<PermissionModel?> GetByResourceAndAction(string resource, string action);
    Task<List<PermissionModel>> GetByRoleId(long roleId);
}
