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

    public async Task<ParkingLotModel> GetParkingLotById(int id)
    {
        ParkingLotModel? parkingLot = await _dataAccess.ParkingLots.GetById(id);
        if (parkingLot is null) throw new KeyNotFoundException("Parking lot not found");

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
}