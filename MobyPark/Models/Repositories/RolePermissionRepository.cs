using Microsoft.EntityFrameworkCore;

using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class RolePermissionRepository : Repository<RolePermissionModel>, IRolePermissionRepository
{
    public RolePermissionRepository(AppDbContext context) : base(context) { }

    public async Task<bool> RoleHasPermission(long roleId, long permissionId) =>
        await DbSet.AnyAsync(rolePermission =>
            rolePermission.RoleId == roleId && rolePermission.PermissionId == permissionId);

    public async Task<List<RolePermissionModel>> GetPermissionsByRoleId(long roleId) =>
        await DbSet.Where(rolePermission => rolePermission.RoleId == roleId).ToListAsync();

    public async Task<List<RolePermissionModel>> GetRolesByPermissionId(long permissionId) =>
        await DbSet.Where(rolePermission => rolePermission.PermissionId == permissionId).ToListAsync();

    public async Task<bool> AddPermissionToRole(RolePermissionModel rolePermission)
    {
        await DbSet.AddAsync(rolePermission);
        int success = await Context.SaveChangesAsync();
        return success > 0;
    }

    public async Task<bool> RemovePermissionFromRole(RolePermissionModel rolePermission)
    {
        DbSet.Remove(rolePermission);
        int success = await Context.SaveChangesAsync();
        return success > 0;
    }
}