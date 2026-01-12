using Microsoft.EntityFrameworkCore;

using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class RoleRepository : Repository<RoleModel>, IRoleRepository
{
    public RoleRepository(AppDbContext context) : base(context) { }

    public async Task<RoleModel?> GetByName(string roleName) =>
        await DbSet.FirstOrDefaultAsync(role => role.Name == roleName);

    public async Task<bool> AnyUsersAssigned(long roleId) => await Context.Users.AnyAsync(user => user.RoleId == roleId);

    public async Task<bool> DeleteRole(RoleModel role)
    {
        DbSet.Remove(role);
        int entriesWritten = await Context.SaveChangesAsync();
        return entriesWritten > 0;
    }
}