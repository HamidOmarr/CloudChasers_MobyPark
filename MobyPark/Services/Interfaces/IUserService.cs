using MobyPark.DTOs.User.Request;
using MobyPark.Models;
using MobyPark.Services.Results.User;

namespace MobyPark.Services.Interfaces;

public interface IUserService
{
    Task<UserModel?> GetUserByUsername(string username);
    Task<UserModel?> GetUserByEmail(string email);
    Task<UserModel?> GetUserById(long userId);
    Task<List<UserModel>> GetAllUsers();
    Task<int> CountUsers();
    Task<bool> DeleteUser(long id);
    Task<RegisterResult> CreateUserAsync(RegisterDto dto);
    Task<LoginResult> LoginAsync(LoginDto dto);
    Task<UpdateProfileResult> UpdateUserProfileAsync(UserModel user, UpdateProfileDto dto);
    Task<UpdateProfileResult> UpdateUserIdentityAsync(long userId, string? newFirstName, string? newLastName, DateOnly? newBirthday);
    Task<UpdateProfileResult> UpdateUserRoleAsync(long userId, long roleId);
}