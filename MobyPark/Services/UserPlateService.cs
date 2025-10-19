using MobyPark.Models;
using MobyPark.Models.Repositories;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Validation;

namespace MobyPark.Services;

public class UserPlateService
{
    private readonly IUserPlateRepository _userPlates;

    public UserPlateService(IRepositoryStack repoStack)
    {
        _userPlates = repoStack.UserPlates;
    }

    public async Task<UserPlateModel> CreateUserPlate(UserPlateModel userPlate)
    {
        ServiceValidator.UserPlate(userPlate);

        (bool createdSuccessfully, long id) = await _userPlates.CreateWithId(userPlate);
        if (createdSuccessfully) userPlate.Id = id;
        return userPlate;
    }

    public async Task<UserPlateModel?> GetUserPlateById(long id) => await _userPlates.GetById<UserPlateModel>(id);

    public async Task<List<UserPlateModel>> GetUserPlatesByUserId(long userId)
    {
        var userPlates = await _userPlates.GetPlatesByUserId(userId);
        if ( userPlates.Count == 0) throw new KeyNotFoundException($"No license plates found for user with ID {userId}.");

        return userPlates;
    }

    public async Task<List<UserPlateModel>> GetUserPlatesByPlate(string plate)
    {
        plate = ValHelper.NormalizePlate(plate);
        var userPlates =  await _userPlates.GetPlatesByPlate(plate);
        if (userPlates.Count == 0) throw new KeyNotFoundException($"No license plates found with plate number '{plate}'.");

        return userPlates;
    }

    public async Task<UserPlateModel> GetPrimaryUserPlateByUserId(long userId)
    {
        var primaryPlate = await _userPlates.GetPrimaryPlateByUserId(userId);
        if (primaryPlate is null) throw new KeyNotFoundException($"No primary license plate found for user with ID {userId}.");

        return primaryPlate;
    }

    public async Task<UserPlateModel> GetUserPlateByUserIdAndPlate(long userId, string plate)
    {
        var userPlate = await _userPlates.GetByUserIdAndPlate(userId, plate);
        if (userPlate is null) throw new KeyNotFoundException($"No license plate '{plate}' found for user with ID {userId}.");

        return userPlate;
    }

    public async Task<List<UserPlateModel>> GetAllUserPlates() => await _userPlates.GetAll();

    private async Task<bool> CheckUserPlatePairExist(string userIdString, string plateNumber)
    {
        if (!long.TryParse(userIdString, out long userId))
            throw new ArgumentException("User ID must be a valid long.", nameof(userIdString));

        return await _userPlates.Exists(plate =>
            plate.UserId == userId && plate.LicensePlateNumber == plateNumber);
    }

    public async Task<bool> UserPlateExists(string checkBy, string[] filterValue)
    {
        if (filterValue is null || filterValue.Length == 0)
            throw new ArgumentException("Filter value cannot be empty.", nameof(filterValue));

        bool exists = checkBy.ToLower() switch
        {
            "id" => filterValue.All(value =>
                long.TryParse(value, out long id) &&
                _userPlates.Exists(plate => plate.Id == id).Result),

            "userplate" => filterValue.Length == 2
                ? await CheckUserPlatePairExist(filterValue[0], filterValue[1])
                : throw new ArgumentException("When checking by 'userplate', exactly two values are required: userId and plateNumber."),

            _ => throw new ArgumentException("Invalid checkBy parameter. Must be 'id' or 'userplate'.", nameof(checkBy))
        };

        return exists;
    }

    public async Task<int> GetUserPlatesCount() => await _userPlates.Count();

    public async Task<bool> ChangePrimaryUserPlate(long userId, string newPrimaryPlate)
    {
        var currentPrimary = await GetPrimaryUserPlateByUserId(userId);
        if (currentPrimary.LicensePlateNumber == newPrimaryPlate) throw new InvalidOperationException("The specified plate is already the primary plate.");

        var newPrimary = await GetUserPlateByUserIdAndPlate(userId, newPrimaryPlate);
        if (newPrimary is null) throw new KeyNotFoundException("The specified new primary plate does not exist for the user.");

        currentPrimary.IsPrimary = false;
        newPrimary.IsPrimary = true;

        var updatedCurrent = await UpdateUserPlate(currentPrimary);
        var updatedNew = await UpdateUserPlate(newPrimary);

        return updatedCurrent && updatedNew;
    }

    public async Task<bool> UpdateUserPlate(UserPlateModel userPlate)
    {
        ServiceValidator.UserPlate(userPlate);

        bool updatedSuccessfully = await _userPlates.Update(userPlate);
        return updatedSuccessfully;
    }

    public async Task<bool> DeleteUserPlate(UserPlateModel userPlate)
    {
        ServiceValidator.UserPlate(userPlate);

        bool deletedSuccessfully = await _userPlates.Delete(userPlate);
        return deletedSuccessfully;
    }

    public async Task<bool> RemoveUserPlate(long userId, string plate)
    {
        var plateToRemove = await GetUserPlateByUserIdAndPlate(userId, plate);
        if (plateToRemove is null) throw new KeyNotFoundException("The specified plate does not exist for the user.");

        var allPlates = await GetUserPlatesByUserId(userId);
        if (allPlates.Count <= 1) throw new InvalidOperationException("Cannot remove the only license plate for the user.");

        bool updatedPrimary = true;

        if (plateToRemove.IsPrimary)
        {
            var newPrimary = allPlates.FirstOrDefault(userPlate => userPlate.Id != plateToRemove.Id);
            if (newPrimary is null) throw new InvalidOperationException("No alternate plate found to assign as primary.");

            newPrimary.IsPrimary = true;
            updatedPrimary = await UpdateUserPlate(newPrimary);
        }

        plateToRemove.UserId = UserPlateRepository.DeletedUserId;
        plateToRemove.IsPrimary = false;

        bool removedSuccessfully = await UpdateUserPlate(plateToRemove);

        return removedSuccessfully && updatedPrimary;
    }
}
