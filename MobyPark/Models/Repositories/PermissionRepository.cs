using Microsoft.EntityFrameworkCore;

using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class PermissionRepository : Repository<PermissionModel>, IPermissionRepository
{
    public PermissionRepository(AppDbContext context) : base(context) { }

    public async Task<PermissionModel?> GetByResourceAndAction(string resource, string action) =>
        await DbSet.FirstOrDefaultAsync(permission => permission.Resource == resource && permission.Action == action);

    public async Task<List<PermissionModel>> GetByRoleId(long roleId)
    {
        return await Context.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == roleId)
            .Include(rolePermission => rolePermission.Permission)
            .Select(rolePermission => rolePermission.Permission)
            .ToListAsync();
    }
}