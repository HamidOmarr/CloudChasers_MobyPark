namespace MobyPark.Models.Repositories.Interfaces;

public interface IRolePermissionRepository : IRepository<RolePermissionModel>
{
    Task<bool> RoleHasPermission(long roleId, long permissionId);
    Task<List<RolePermissionModel>> GetPermissionsByRoleId(long roleId);
    Task<List<RolePermissionModel>> GetRolesByPermissionId(long permissionId);
    Task<bool> AddPermissionToRole(RolePermissionModel rolePermission);
    Task<bool> RemovePermissionFromRole(RolePermissionModel rolePermission);
}