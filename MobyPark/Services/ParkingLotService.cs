using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.ParkingLot;

namespace MobyPark.Services;

public class ParkingLotService : IParkingLotService
{
    private readonly IParkingLotRepository _parkingLots;

    public ParkingLotService(IParkingLotRepository parkingLots)
    {
        _parkingLots = parkingLots;
    }

    public async Task<CreateLotResult> CreateParkingLot(ParkingLotModel lot)
    {
        try
        {
            (bool createdSuccessfully, long id) = await _parkingLots.CreateWithId(lot);
            if (!createdSuccessfully)
                return new CreateLotResult.Error("Database insertion failed.");

            lot.Id = id;
            return new CreateLotResult.Success(lot);
        }
        catch (Exception ex)
        { return new CreateLotResult.Error(ex.Message); }
    }

    public async Task<GetLotResult> GetParkingLotById(long id)
    {
        var lot = await _parkingLots.GetById<ParkingLotModel>(id);
        if (lot is null)
            return new GetLotResult.NotFound();

        return new GetLotResult.Success(lot);
    }

    public async Task<GetLotResult> GetParkingLotByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new GetLotResult.InvalidInput("Name cannot be empty or whitespace.");

        var lot = await _parkingLots.GetByName(name);
        if (lot is null)
            return new GetLotResult.NotFound();

        return new GetLotResult.Success(lot);
    }

    public async Task<GetLotListResult> GetParkingLotsByLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return new GetLotListResult.InvalidInput("Location cannot be empty or whitespace.");

        var lots = await _parkingLots.GetByLocation(location);

        if (lots.Count == 0)
            return new GetLotListResult.NotFound();

        return new GetLotListResult.Success(lots);
    }

    public async Task<GetLotListResult> GetAllParkingLots()
    {
        var lots = await _parkingLots.GetAll();

        if (lots.Count == 0)
            return new GetLotListResult.NotFound();

        return new GetLotListResult.Success(lots);
    }

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

    public async Task<UpdateLotResult> UpdateParkingLot(ParkingLotModel lot)
    {
        var getResult = await GetParkingLotById(lot.Id);
        if (getResult is GetLotResult.NotFound)
            return new UpdateLotResult.NotFound();

        try
        {
            if (!await _parkingLots.Update(lot))
                return new UpdateLotResult.Error("Database update failed.");

            return new UpdateLotResult.Success(lot);
        }
        catch (Exception ex)
        { return new UpdateLotResult.Error(ex.Message); }
    }

    public async Task<DeleteLotResult> DeleteParkingLot(long id)
    {
        var getResult = await GetParkingLotById(id);
        if (getResult is GetLotResult.NotFound)
            return new DeleteLotResult.NotFound();

        var lot = ((GetLotResult.Success)getResult).Lot;

        try
        {
            if (!await _parkingLots.Delete(lot))
                return new DeleteLotResult.Error("Database deletion failed.");

            return new DeleteLotResult.Success();
        }
        catch (Exception ex)
        { return new DeleteLotResult.Error(ex.Message); }
    }
}
