using MobyPark.Services;

namespace MobyPark.Models.Access;

public interface IUserAccess : IRepository<UserModel>
{
    Task<UserModel?> GetByUsername(string username);
    Task<UserModel?> GetByEmail(string email);
}