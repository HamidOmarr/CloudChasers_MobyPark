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
            { "@daytariff", parkingLot.DayTariff.HasValue ? (object)parkingLot.DayTariff.Value : DBNull.Value },
            { "@created_at", parkingLot.CreatedAt }
        };

        return parameters;
    }

    public ParkingLotAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<ParkingLotModel?> GetByName(string modelName)
    {
        Dictionary<string, object> parameters = new() { { "@modelname", modelName } };

        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE name = @modelname", parameters);

        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

    public async Task<int> AddParkingLotAsync(ParkingLotModel parkingLot)
    {
        const string query = @"
            INSERT INTO parkinglots
                (name, location, address, capacity, reserverd, tariff, daytariff, created_at)
            VALUES
                (@Name, @Location, @Address, @Capacity, @Reserved, @Tariff, @DayTariff, @CreatedAt)
            RETURNING id;";
        
        var parameters = new Dictionary<string, object>
        {
            { "@Name", parkingLot.Name },
            { "@Location", parkingLot.Location},
            { "@Address", parkingLot.Address },
            { "@Capacity", parkingLot.Capacity},
            { "@Reserved", parkingLot.Reserved},
            { "@Tariff", parkingLot.Tariff},
            { "@DayTariff", parkingLot.DayTariff},
            { "@CreatedAt", parkingLot.CreatedAt},
        };
        var result = await Connection.ExecuteScalar(query, parameters);

        return Convert.ToInt32(result);
    }
}
