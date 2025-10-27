using MobyPark.Models;
using MobyPark.Services.Results.UserPlate;

namespace MobyPark.Services.Interfaces;

public interface IUserPlateService
{
    Task<CreateUserPlateResult> CreateUserPlate(UserPlateModel userPlate);
    Task<CreateUserPlateResult> AddLicensePlateToUser(long userId, string plate);
    Task<GetUserPlateResult> GetUserPlateById(long id);
    Task<GetUserPlateListResult> GetUserPlatesByUserId(long userId);
    Task<GetUserPlateListResult> GetUserPlatesByPlate(string plate);
    Task<GetUserPlateResult> GetPrimaryUserPlateByUserId(long userId);
    Task<GetUserPlateResult> GetUserPlateByUserIdAndPlate(long userId, string plate);
    Task<GetUserPlateListResult> GetAllUserPlates();
    Task<UserPlateExistsResult> UserPlateExists(string checkBy, string[] filterValue);
    Task<int> GetUserPlatesCount();
    Task<UpdateUserPlateResult> ChangePrimaryUserPlate(long userId, string newPrimaryPlate);
    Task<UpdateUserPlateResult> UpdateUserPlate(UserPlateModel userPlate);
    Task<DeleteUserPlateResult> DeleteUserPlate(UserPlateModel userPlate);
    Task<DeleteUserPlateResult> RemoveUserPlate(long userId, string plate);
}
