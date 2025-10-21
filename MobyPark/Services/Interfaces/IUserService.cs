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
    Task<LoginResult> LoginAsync(LoginDto dto);
    Task<UpdateProfileResult> UpdateUserProfileAsync(UserModel user, UpdateProfileDto dto);
    Task<UpdateProfileResult> UpdateUserIdentityAsync(long userId, string? newFirstName, string? newLastName, DateOnly? newBirthday);
    Task<UpdateProfileResult> UpdateUserRoleAsync(long userId, long roleId);
}