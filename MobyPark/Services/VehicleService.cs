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

    public async Task<List<VehicleModel>> GetVehicleByUserId(int userId)
    {
        List<VehicleModel> vehicles = await _dataAccess.Vehicles.GetByUserId(userId);
        if (vehicles.Count == 0) throw new KeyNotFoundException("No vehicles found");

        return vehicles;
    }

    public async Task<VehicleModel> GetVehicleByLicensePlate(string licensePlate)
    {
        VehicleModel? vehicle = await _dataAccess.Vehicles.GetByLicensePlate(licensePlate);
        if (vehicle is null) throw new KeyNotFoundException("Vehicle not found");

        return vehicle;
    }

    public async Task<VehicleModel?> GetVehicleByUserIdAndLicense(int userId, string licensePlate)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(licensePlate);

        VehicleModel? vehicle = await _dataAccess.Vehicles.GetByUserAndLicense(userId, licensePlate);
        return vehicle;
    }

    public async Task<VehicleModel> CreateVehicle(VehicleModel vehicle)
    {
        Dictionary<string, object?> parameters = new()
        {
            { nameof(vehicle.LicensePlate), vehicle.LicensePlate },
            { nameof(vehicle.Make), vehicle.Make },
            { nameof(vehicle.Model), vehicle.Model },
            { nameof(vehicle.Color), vehicle.Color }
        };

        foreach (var param in parameters)
            ArgumentNullException.ThrowIfNull(param.Value, param.Key);

        if (vehicle.UserId <= 0)
            throw new ArgumentOutOfRangeException(nameof(vehicle.UserId), "UserId must be greater than 0.");
        if (vehicle.Year <= 0)
            throw new ArgumentOutOfRangeException(nameof(vehicle.Year), "Year must be greater than 0.");

        await _dataAccess.Vehicles.Create(vehicle);
        return vehicle;
    }

    public async Task<VehicleModel> UpdateVehicle(int userId, string licensePlate, string make, string model,
        string color, int year)
    {
        Dictionary<string, object?> parameters = new()
        {
            { nameof(licensePlate), licensePlate },
            { nameof(make), make },
            { nameof(model), model },
            { nameof(color), color }
        };

        foreach (var param in parameters)
            ArgumentNullException.ThrowIfNull(param.Value, param.Key);

        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId), "UserId must be greater than 0.");
        if (year <= 0)
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than 0.");

        VehicleModel vehicle = new()
        {
            UserId = userId,
            LicensePlate = licensePlate,
            Make = make,
            Model = model,
            Color = color,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };

        await _dataAccess.Vehicles.Update(vehicle);
        return vehicle;
    }

    public async Task<bool> DeleteVehicle(int id)
    {
        await GetVehicleById(id);  // to ensure the vehicle exists beforehand.


        bool success = await _dataAccess.Vehicles.Delete(id);
        return success;
    }
}