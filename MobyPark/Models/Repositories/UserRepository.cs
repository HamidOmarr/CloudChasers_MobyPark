using Microsoft.EntityFrameworkCore;
using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class UserRepository : Repository<UserModel>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<UserModel?> GetByUsername(string username) =>
        await DbSet.FirstOrDefaultAsync(user => user.Username == username);

    public async Task<UserModel?> GetByEmail(string email) =>
        await DbSet.FirstOrDefaultAsync(user => user.Email == email);

    public async Task<List<UserModel>> GetUsersByRole(string roleName)
    {
        return await DbSet
            .Where(user => user.Role.Name == roleName)
            .ToListAsync();
    }

    public async Task<UserModel> GetByIdWithRoleAndPermissions(long id) =>
        await DbSet
            .Include(user => user.Role)
                .ThenInclude(role => role.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstAsync(user => user.Id == id);
}
