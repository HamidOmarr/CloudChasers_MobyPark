namespace MobyPark.Models.Repositories.Interfaces;

public interface IUserRepository : IRepository<UserModel>
{
    Task<UserModel?> GetByUsername(string username);
    Task<UserModel?> GetByEmail(string email);
    Task<UserModel> GetByIdWithRoleAndPermissions(long id);
}
