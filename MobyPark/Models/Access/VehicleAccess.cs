using Microsoft.Data.Sqlite;
using MobyPark.Services.DatabaseConnection;

namespace MobyPark.Models.Access;

public class VehicleAccess : Repository<VehicleModel>, IVehicleAccess
{
    protected override string TableName => "vehicles";
    protected override VehicleModel MapFromReader(SqliteDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(VehicleModel vehicle)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@id", vehicle.Id},
            { "@user_id", vehicle.UserId },
            { "@license_plate", vehicle.LicensePlate },
            { "@make", vehicle.Make },
            { "@model", vehicle.Model },
            { "@color", vehicle.Color },
            { "@year", vehicle.Year },
            { "@created_at", vehicle.CreatedAt.ToString("yyyy-MM-dd") }
        };

        return parameters;
    }

    public VehicleAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<List<VehicleModel>> GetByUserId(int userId)
    {
    var parameters = new Dictionary<string, object> { { "@user_id", userId } };
    var vehicles = new List<VehicleModel>();
    await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE user_id = @user_id", parameters);

        while (await reader.ReadAsync())
            vehicles.Add(MapFromReader(reader));

        return vehicles;
    }

    public async Task<VehicleModel?> GetByLicensePlate(string licensePlate)
    {
        var parameters = new Dictionary<string, object> { { "@license_plate", licensePlate } };
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE license_plate = @license_plate", parameters);
        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

    public async Task<VehicleModel?> GetByUserAndLicense(int userId, string licensePlate)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@user_id", userId },
            { "@license_plate", licensePlate }
        };

        await using var reader = await Connection.ExecuteQuery(
            $"SELECT * FROM {TableName} WHERE user_id = @user_id AND license_plate = @license_plate", parameters);
        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }
}
