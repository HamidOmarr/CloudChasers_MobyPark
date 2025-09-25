using Npgsql;
using MobyPark.Models.Access.DatabaseConnection;

namespace MobyPark.Models.Access;

public class ParkingLotAccess : Repository<ParkingLotModel>, IParkingLotAccess
{
    protected override string TableName => "ParkingLots";
    protected override ParkingLotModel MapFromReader(NpgsqlDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(ParkingLotModel parkingLot)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@id", parkingLot.Id },
            { "@Name", parkingLot.Name },
            { "@Location", parkingLot.Location },
            { "@Address", parkingLot.Address },
            { "@Capacity", parkingLot.Capacity },
            { "@Reserved", parkingLot.Reserved },
            { "@Tariff", parkingLot.Tariff },
            { "@DayTariff", parkingLot.DayTariff },
            { "@CreatedAt", parkingLot.CreatedAt.ToString("yyyy-MM-dd") },
            { "@Lat", parkingLot.Coordinates.Lat },
            { "@Lng", parkingLot.Coordinates.Lng }
        };

        return parameters;
    }

    public ParkingLotAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<ParkingLotModel?> GetByName(string modelName)
    {
        Dictionary<string, object> parameters = new() { { "@Model", modelName } };

        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE Model = @Model", parameters);

        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }
}
