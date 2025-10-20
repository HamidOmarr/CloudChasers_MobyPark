using MobyPark.Models;

namespace MobyPark.Services.Interfaces;

public interface IUserPlateService
{
    Task<UserPlateModel> CreateUserPlate(UserPlateModel userPlate);
    Task<bool> AddLicensePlateToUser(long userId, string plate);
    Task<UserPlateModel?> GetUserPlateById(long id);
    Task<List<UserPlateModel>> GetUserPlatesByUserId(long userId);
    Task<List<UserPlateModel>> GetUserPlatesByPlate(string plate);
    Task<UserPlateModel> GetPrimaryUserPlateByUserId(long userId);
    Task<UserPlateModel?> GetUserPlateByUserIdAndPlate(long userId, string plate);
    Task<List<UserPlateModel>> GetAllUserPlates();
    Task<bool> UserPlateExists(string checkBy, string[] filterValue);
    Task<int> GetUserPlatesCount();
    Task<bool> ChangePrimaryUserPlate(long userId, string newPrimaryPlate);
    Task<bool> UpdateUserPlate(UserPlateModel userPlate);
    Task<bool> DeleteUserPlate(UserPlateModel userPlate);
    Task<bool> RemoveUserPlate(long userId, string plate);
}
