using Npgsql;
using MobyPark.Models.Access.DatabaseConnection;

namespace MobyPark.Models.Access;

public class VehicleAccess : Repository<VehicleModel>, IVehicleAccess
{
    protected override string TableName => "Vehicles";
    protected override VehicleModel MapFromReader(NpgsqlDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(VehicleModel vehicle)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@id", vehicle.Id},
            { "@UserId", vehicle.UserId },
            { "@LicensePlate", vehicle.LicensePlate },
            { "@Make", vehicle.Make },
            { "@Model", vehicle.Model },
            { "@Color", vehicle.Color },
            { "@Year", vehicle.Year },
            { "@CreatedAt", vehicle.CreatedAt }
        };

        return parameters;
    }

    public VehicleAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<List<VehicleModel>> GetByUserId(int userId)
    {
        var parameters = new Dictionary<string, object> { { "@UserId", userId } };
        var vehicles = new List<VehicleModel>();
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE UserId = @UserId", parameters);

        while (await reader.ReadAsync())
            vehicles.Add(MapFromReader(reader));

        return vehicles;
    }

    public async Task<VehicleModel?> GetByLicensePlate(string licensePlate)
    {
        var parameters = new Dictionary<string, object> { { "@LicensePlate", licensePlate } };
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE LicensePlate = @LicensePlate", parameters);
        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

    public async Task<VehicleModel?> GetByUserAndLicense(int userId, string licensePlate)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@UserId", userId },
            { "@LicensePlate", licensePlate }
        };

        await using var reader = await Connection.ExecuteQuery(
            $"SELECT * FROM {TableName} WHERE UserId = @UserId AND LicensePlate = @LicensePlate", parameters);
        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }
}
