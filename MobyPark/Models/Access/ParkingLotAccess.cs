using Microsoft.Data.Sqlite;
using MobyPark.Services.DatabaseConnection;

namespace MobyPark.Models.Access;

public class ParkingLotAccess : Repository<ParkingLotModel>, IParkingLotAccess
{
    protected override string TableName => "parking_lots";
    protected override ParkingLotModel MapFromReader(SqliteDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(ParkingLotModel parkingLot)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@id", parkingLot.Id },
            { "@name", parkingLot.Name },
            { "@location", parkingLot.Location },
            { "@address", parkingLot.Address },
            { "@capacity", parkingLot.Capacity },
            { "@reserved", parkingLot.Reserved },
            { "@tarrif", parkingLot.Tariff },
            { "@daytarrif", parkingLot.DayTariff },
            { "@created_at", parkingLot.CreatedAt.ToString("yyyy-MM-dd") },
            { "@lat", parkingLot.Coordinates.Lat },
            { "@lng", parkingLot.Coordinates.Lng }
        };

        return parameters;
    }

    public ParkingLotAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<ParkingLotModel?> GetByName(string modelName)
    {
        Dictionary<string, object> parameters = new() { { "@name", modelName } };

        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE name = @name", parameters);

        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }
}
