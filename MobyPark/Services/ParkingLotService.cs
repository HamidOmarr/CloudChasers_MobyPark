using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Validation;

namespace MobyPark.Services;

public class ParkingLotService
{
    private readonly IParkingLotRepository _parkingLots;

    public ParkingLotService(IRepositoryStack repoStack)
    {
        _parkingLots = repoStack.ParkingLots;
    }

    public async Task<ParkingLotModel> CreateParkingLot(ParkingLotModel lot)
    {
        ServiceValidator.ParkingLot(lot);

        (bool createdSuccessfully, long id) = await _parkingLots.CreateWithId(lot);
        if (createdSuccessfully) lot.Id = id;
        return lot;
    }

    public async Task<ParkingLotModel?> GetParkingLotById(long id) => await _parkingLots.GetById<ParkingLotModel>(id);

    public async Task<ParkingLotModel?> GetParkingLotByName(string name)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty or whitespace.", nameof(name));

        return await _parkingLots.GetByName(name);
    }

    public async Task<List<ParkingLotModel>> GetParkingLotsByLocation(string location)
    {
        ArgumentNullException.ThrowIfNull(location, nameof(location));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location cannot be empty or whitespace.", nameof(location));

        var lots = await _parkingLots.GetByLocation(location);

        if (lots.Count == 0)
            throw new KeyNotFoundException("No parking lots found for the specified location.");

        return lots;
    }

    public async Task<List<ParkingLotModel>> GetAllParkingLots() => await _parkingLots.GetAll();

    public async Task<bool> ParkingLotExists(string checkBy, string filterValue)
    {
        if (string.IsNullOrWhiteSpace(filterValue))
            throw new ArgumentException("Filter value cannot be empty or whitespace.", nameof(filterValue));

        bool exists = checkBy.ToLower() switch
        {
            "id" => long.TryParse(filterValue, out long id) && await _parkingLots.Exists(lot => lot.Id == id),
            "address" => await _parkingLots.Exists(lot => lot.Address == filterValue),
            _ => throw new ArgumentException("Invalid checkBy parameter. Must be 'id', or 'address'.",
                nameof(checkBy))
        };

        return exists;
    }

    public async Task<int> CountParkingLots() => await _parkingLots.Count();

    public async Task<bool> UpdateParkingLot(ParkingLotModel lot)
    {
        var existingLot = await GetParkingLotById(lot.Id);
        if (existingLot is null) throw new KeyNotFoundException("Parking lot not found");

        ServiceValidator.ParkingLot(lot);

        bool updatedSuccessfully = await _parkingLots.Update(lot);
        return updatedSuccessfully;
    }

    public async Task<bool> DeleteParkingLot(long id)
    {
        ParkingLotModel? lot = await GetParkingLotById(id);
        if (lot is null) throw new KeyNotFoundException("Parking lot not found");

        bool deletedSuccessfully = await _parkingLots.Delete(lot);
        return deletedSuccessfully;
    }
}
