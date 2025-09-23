using MobyPark.Models;
using MobyPark.Models.DataService;

namespace MobyPark.Services;

public class VehicleService
{
    private readonly IDataAccess _dataAccess;

    public VehicleService(IDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public async Task<VehicleModel> GetVehicleById(int id)
    {
        VehicleModel? vehicle = await _dataAccess.Vehicles.GetById(id);
        if (vehicle is null) throw new KeyNotFoundException("Vehicle not found");

        return vehicle;
    }

    public async Task<VehicleModel> GetVehicleByLicensePlate(string licensePlate)
    {
        VehicleModel? vehicle = await _dataAccess.Vehicles.GetByLicensePlate(licensePlate);
        if (vehicle is null) throw new KeyNotFoundException("Vehicle not found");

        return vehicle;
    }
}