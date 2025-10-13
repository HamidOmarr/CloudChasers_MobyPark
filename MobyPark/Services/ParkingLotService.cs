using MobyPark.Models;
using MobyPark.Models.DataService;
using MobyPark.Services.Results.ParkingLot;

namespace MobyPark.Services;

public class ParkingLotService
{
    private readonly IDataAccess _dataAccess;
    private readonly SessionService _sessions;

    public ParkingLotService(IDataAccess dataAccess, SessionService sessions)
    {
        _dataAccess = dataAccess;
        _sessions = sessions;
    }

    public async Task<ParkingLotModel?> GetParkingLotByAddress(string address) =>
        await _dataAccess.ParkingLots.GetParkingLotByAddress(address);
    
    public async Task<ParkingLotModel?> GetParkingLotById(int id) =>
        await _dataAccess.ParkingLots.GetParkingLotByID(id);


    public async Task<RegisterResult> InsertParkingLotAsync(ParkingLotModel parkingLot)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(parkingLot.Name) ||
            string.IsNullOrWhiteSpace(parkingLot.Location) ||
            string.IsNullOrWhiteSpace(parkingLot.Address))
            return new RegisterResult.InvalidData("Missing required fields");

        if (parkingLot.Capacity < 0 || parkingLot.Tariff < 0 || parkingLot.DayTariff < 0)
            return new RegisterResult.InvalidData("Capacity and tariffs must be at least 0");

        if (parkingLot.Reserved < 0 || parkingLot.Reserved > parkingLot.Capacity)
            return new RegisterResult.InvalidData("Reserved must be between 0 and Capacity");
        
        if (await GetParkingLotByAddress(parkingLot.Address) is not null)
            return new RegisterResult.AddressTaken();

        var id = await _dataAccess.ParkingLots.AddParkingLotAsync(parkingLot);
        if (id <= 0) return new RegisterResult.Error("Failed to create parking lot");

        parkingLot.Id = id;
        return new RegisterResult.Success(parkingLot);
    }
    
}
