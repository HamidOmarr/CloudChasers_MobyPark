using MobyPark.DTOs.User.Request;
using MobyPark.Models;
using MobyPark.Services.Results.User;

namespace MobyPark.Services.Interfaces;

public interface IUserService
{
    Task<GetUserResult> GetUserByUsername(string username);
    Task<GetUserResult> GetUserByEmail(string email);
    Task<GetUserResult> GetUserById(long userId);
    Task<GetUserListResult> GetAllUsers();
    Task<int> CountUsers();
    Task<DeleteUserResult> DeleteUser(long id);
    Task<RegisterResult> CreateUserAsync(RegisterDto dto);
    Task<LoginResult> Login(LoginDto dto);
    Task<UpdateUserResult> UpdateUserProfile(long userId, UpdateUserDto dto);
    Task<UpdateUserResult> UpdateUserIdentity(long userId, UpdateUserIdentityDto dto);
    Task<UpdateUserResult> UpdateUserRole(long userId, UpdateUserRoleDto dto);
}
