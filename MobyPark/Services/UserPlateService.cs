using MobyPark.Models;
using MobyPark.Models.Repositories;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.UserPlate;
using MobyPark.Validation;

namespace MobyPark.Services;

public class UserPlateService : IUserPlateService
{
    private readonly IUserPlateRepository _userPlates;

    public UserPlateService(IUserPlateRepository userPlates)
    {
        _userPlates = userPlates;
    }

    public async Task<CreateUserPlateResult> CreateUserPlate(UserPlateModel userPlate)
    {
        try
        {
            (bool createdSuccessfully, long id) = await _userPlates.CreateWithId(userPlate);
            if (!createdSuccessfully)
                return new CreateUserPlateResult.Error("Database insertion failed.");

            userPlate.Id = id;
            return new CreateUserPlateResult.Success(userPlate);
        }
        catch (Exception ex)
        { return new CreateUserPlateResult.Error(ex.Message); }
    }

    public async Task<CreateUserPlateResult> AddLicensePlateToUser(long userId, string plate)
    {
        plate = plate.Upper();

        var existingResult = await GetUserPlateByUserIdAndPlate(userId, plate);
        if (existingResult is GetUserPlateResult.Success)
            return new CreateUserPlateResult.AlreadyExists();

        var allPlatesResult = await GetUserPlatesByUserId(userId);
        bool isFirstPlate = allPlatesResult is GetUserPlateListResult.NotFound;

        var userPlate = new UserPlateModel
        {
            UserId = userId,
            LicensePlateNumber = plate,
            CreatedAt = DateTimeOffset.UtcNow,
            IsPrimary = isFirstPlate
        };

        return await CreateUserPlate(userPlate);
    }

    public async Task<GetUserPlateResult> GetUserPlateById(long id)
    {
        var userPlate = await _userPlates.GetById<UserPlateModel>(id);
        if (userPlate is null)
            return new GetUserPlateResult.NotFound();
        return new GetUserPlateResult.Success(userPlate);
    }

    public async Task<GetUserPlateListResult> GetUserPlatesByUserId(long userId)
    {
        var userPlates = await _userPlates.GetPlatesByUserId(userId);
        if (userPlates.Count == 0)
            return new GetUserPlateListResult.NotFound();
        return new GetUserPlateListResult.Success(userPlates);
    }

    public async Task<GetUserPlateListResult> GetUserPlatesByPlate(string plate)
    {
        plate = plate.Upper();
        var userPlates = await _userPlates.GetPlatesByPlate(plate);
        if (userPlates.Count == 0)
            return new GetUserPlateListResult.NotFound();
        return new GetUserPlateListResult.Success(userPlates);
    }

    public async Task<GetUserPlateResult> GetPrimaryUserPlateByUserId(long userId)
    {
        var primaryPlate = await _userPlates.GetPrimaryPlateByUserId(userId);
        if (primaryPlate is null)
            return new GetUserPlateResult.NotFound();
        return new GetUserPlateResult.Success(primaryPlate);
    }

    public async Task<GetUserPlateResult> GetUserPlateByUserIdAndPlate(long userId, string plate)
    {
        var userPlate = await _userPlates.GetByUserIdAndPlate(userId, plate);
        if (userPlate is null)
            return new GetUserPlateResult.NotFound();
        return new GetUserPlateResult.Success(userPlate);
    }

    public async Task<GetUserPlateListResult> GetAllUserPlates()
    {
        var plates = await _userPlates.GetAll();
        if (plates.Count == 0)
            return new GetUserPlateListResult.NotFound();
        return new GetUserPlateListResult.Success(plates);
    }

    public async Task<UserPlateExistsResult> UserPlateExists(string checkBy, string[] filterValue)
    {
        if (filterValue.Length == 0)
            return new UserPlateExistsResult.InvalidInput("Filter value cannot be empty.");

        UserPlateExistsResult FromBool(bool exists) => exists ? new UserPlateExistsResult.Exists() : new UserPlateExistsResult.NotExists();

        checkBy = checkBy.Trim().ToLowerInvariant();

        if (checkBy == "id")
        {
            return !long.TryParse(filterValue[0], out long id)
                ? new UserPlateExistsResult.InvalidInput("ID must be a valid long.")
                : FromBool(await _userPlates.Exists(plate => plate.Id == id));
        }

        if (checkBy == "userplate")
        {
            if (filterValue.Length != 2)
                return new UserPlateExistsResult.InvalidInput("When checking by 'userplate', exactly two values are required: userId and plateNumber.");

            if (!long.TryParse(filterValue[0], out var userId))
                return new UserPlateExistsResult.InvalidInput("User ID must be a valid long.");

            return FromBool(await _userPlates.Exists(
                plate => plate.UserId == userId && plate.LicensePlateNumber == filterValue[1]));
        }

        return new UserPlateExistsResult.InvalidInput("Invalid checkBy parameter. Must be 'id' or 'userplate'.");
    }

    public async Task<int> GetUserPlatesCount() => await _userPlates.Count();

    public async Task<UpdateUserPlateResult> ChangePrimaryUserPlate(long userId, string newPrimaryPlate)
    {
        var currentPrimaryResult = await GetPrimaryUserPlateByUserId(userId);
        if (currentPrimaryResult is GetUserPlateResult.NotFound)
            return new UpdateUserPlateResult.InvalidOperation("User has no primary plate to change.");
        var currentPrimary = ((GetUserPlateResult.Success)currentPrimaryResult).Plate;

        if (currentPrimary.LicensePlateNumber == newPrimaryPlate)
            return new UpdateUserPlateResult.InvalidOperation("The specified plate is already the primary plate.");

        var newPrimaryResult = await GetUserPlateByUserIdAndPlate(userId, newPrimaryPlate);
        if (newPrimaryResult is GetUserPlateResult.NotFound)
            return new UpdateUserPlateResult.NotFound();
        var newPrimary = ((GetUserPlateResult.Success)newPrimaryResult).Plate;

        currentPrimary.IsPrimary = false;
        newPrimary.IsPrimary = true;

        var updateCurrentResult = await UpdateUserPlate(currentPrimary);
        if (updateCurrentResult is not UpdateUserPlateResult.Success)
            return updateCurrentResult;

        var updateNewResult = await UpdateUserPlate(newPrimary);
        if (updateNewResult is UpdateUserPlateResult.Success)
            return new UpdateUserPlateResult.Success(newPrimary);

        currentPrimary.IsPrimary = true;
        await UpdateUserPlate(currentPrimary);
        return updateNewResult;

    }

    public async Task<UpdateUserPlateResult> UpdateUserPlate(UserPlateModel userPlate)
    {
        var getResult = await GetUserPlateById(userPlate.Id);

        if (getResult is not GetUserPlateResult.Success success)
            return getResult switch
            {
                GetUserPlateResult.NotFound => new UpdateUserPlateResult.NotFound(),
                _ => new UpdateUserPlateResult.Error("Unknown error occurred while retrieving the user plate.")
            };

        try
        {
            var existingPlate = success.Plate;

            bool updated = await _userPlates.Update(existingPlate, userPlate);
            if (!updated)
                return new UpdateUserPlateResult.Error("Database update failed.");
            return new UpdateUserPlateResult.Success(existingPlate);
        }
        catch (Exception ex)
        { return new UpdateUserPlateResult.Error(ex.Message); }
    }

    public async Task<DeleteUserPlateResult> DeleteUserPlate(UserPlateModel userPlate)
    {
        try
        {
            if (!await _userPlates.Delete(userPlate))
                return new DeleteUserPlateResult.Error("Database deletion failed.");
            return new DeleteUserPlateResult.Success();
        }
        catch (Exception ex)
        { return new DeleteUserPlateResult.Error(ex.Message); }
    }

    public async Task<DeleteUserPlateResult> RemoveUserPlate(long userId, string plate)
    {
        var plateToRemoveResult = await GetUserPlateByUserIdAndPlate(userId, plate);
        if (plateToRemoveResult is GetUserPlateResult.NotFound)
            return new DeleteUserPlateResult.NotFound();
        var plateToRemove = ((GetUserPlateResult.Success)plateToRemoveResult).Plate;

        var allPlatesResult = await GetUserPlatesByUserId(userId);
        if (allPlatesResult is GetUserPlateListResult.NotFound)
            return new DeleteUserPlateResult.InvalidOperation("Cannot remove the only license plate for the user.");
        var allPlates = ((GetUserPlateListResult.Success)allPlatesResult).Plates;

        if (allPlates.Count <= 1)
            return new DeleteUserPlateResult.InvalidOperation("Cannot remove the only license plate for the user.");

        if (plateToRemove.IsPrimary)
        {
            var newPrimary = allPlates.FirstOrDefault(p => p.Id != plateToRemove.Id);
            if (newPrimary is null)
                return new DeleteUserPlateResult.InvalidOperation("No alternate plate found to assign as primary.");

            newPrimary.IsPrimary = true;
            var updatePrimaryResult = await UpdateUserPlate(newPrimary);
            if (updatePrimaryResult is not UpdateUserPlateResult.Success)
                return new DeleteUserPlateResult.Error("Failed to set new primary plate.");
        }

        plateToRemove.UserId = UserPlateRepository.DeletedUserId;
        plateToRemove.IsPrimary = false;

        var updateResult = await UpdateUserPlate(plateToRemove);
        if (updateResult is not UpdateUserPlateResult.Success)
            return new DeleteUserPlateResult.Error("Failed to soft-delete plate.");

        return new DeleteUserPlateResult.Success();
    }
}
