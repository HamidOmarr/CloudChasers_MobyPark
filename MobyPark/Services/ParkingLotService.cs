using MobyPark.Models;
using MobyPark.Models.DataService;

namespace MobyPark.Services;

public class ParkingLotService
{
    private readonly IDataAccess _dataAccess;

    public ParkingLotService(IDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public async Task<ParkingLotModel?> GetParkingLotById(int id)
    {
        ParkingLotModel? parkingLot = await _dataAccess.ParkingLots.GetById(id);
        return parkingLot;
    }

    public async Task<ParkingLotModel> CreateParkingLot(ParkingLotModel parkingLot)
    {
        ArgumentNullException.ThrowIfNull(parkingLot, nameof(parkingLot));

        var referenceProperties = new Dictionary<string, object?>
        {
            { nameof(parkingLot.Name), parkingLot.Name },
            { nameof(parkingLot.Location), parkingLot.Location },
            { nameof(parkingLot.Address), parkingLot.Address },
            { nameof(parkingLot.Coordinates), parkingLot.Coordinates }
        };

        foreach (var prop in referenceProperties)
            ArgumentNullException.ThrowIfNull(prop.Value, prop.Key);

        if (parkingLot.Capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(parkingLot.Capacity), "Capacity must be greater than 0.");
        if (parkingLot.Tariff < 0)
            throw new ArgumentOutOfRangeException(nameof(parkingLot.Tariff), "Tariff cannot be negative.");
        if (parkingLot.DayTariff < 0)
            throw new ArgumentOutOfRangeException(nameof(parkingLot.DayTariff), "Day tariff cannot be negative.");

        if (parkingLot.Coordinates.Lat < -90 || parkingLot.Coordinates.Lat > 90)
            throw new ArgumentOutOfRangeException(nameof(parkingLot.Coordinates.Lat), "Latitude must be between -90 and 90.");
        if (parkingLot.Coordinates.Lng < -180 || parkingLot.Coordinates.Lng > 180)
            throw new ArgumentOutOfRangeException(nameof(parkingLot.Coordinates.Lng), "Longitude must be between -180 and 180.");

        await _dataAccess.ParkingLots.Create(parkingLot);
        return parkingLot;
    }

    public async Task<ParkingLotModel> UpdateParkingLot(int id, string name, string location, string address,
        int capacity, int reserved, decimal tariff, decimal dayTariff, DateTime createdAt, CoordinatesModel coordinates)
    {
        ParkingLotModel parkingLot = new()
        {
            Id = id,
            Name = name,
            Location = location,
            Address = address,
            Capacity = capacity,
            Reserved = reserved,
            Tariff = tariff,
            DayTariff = dayTariff,
            CreatedAt = createdAt,
            Coordinates = coordinates
        };

        await _dataAccess.ParkingLots.Update(parkingLot);
        return parkingLot;
    }

    public async Task<bool> DeleteParkingLot(int id)
    {
        ParkingLotModel? lot = await GetParkingLotById(id);
        if (lot is null) throw new KeyNotFoundException("Parking lot not found");

        bool success = await _dataAccess.ParkingLots.Delete(id);
        return success;
    }

    public async Task<List<ParkingLotModel>> GetAllParkingLots() => await _dataAccess.ParkingLots.GetAll();
}
