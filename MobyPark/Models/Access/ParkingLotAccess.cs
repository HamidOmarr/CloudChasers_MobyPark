using Npgsql;
using MobyPark.Models.Access.DatabaseConnection;
using NpgsqlTypes;

namespace MobyPark.Models.Access;

public class ParkingLotAccess : Repository<ParkingLotModel>, IParkingLotAccess
{
    protected override string TableName => "parking_lots";
    protected override ParkingLotModel MapFromReader(NpgsqlDataReader reader) => new(reader);

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
            { "@tariff", parkingLot.Tariff },
            { "@daytariff", parkingLot.DayTariff },
            { "@lat", parkingLot.Coordinates.Lat },
            { "@lng", parkingLot.Coordinates.Lng }
        };
        parameters.Add("@created_at", new NpgsqlParameter("@created_at", NpgsqlDbType.Date)
        {
            Value = parkingLot.CreatedAt
        });

        return parameters;
    }

    public ParkingLotAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<ParkingLotModel?> GetByName(string modelName)
    {
        Dictionary<string, object> parameters = new() { { "@model", modelName } };

        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE model = @model", parameters);

        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }
}
