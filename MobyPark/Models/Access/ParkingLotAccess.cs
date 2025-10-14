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
        string query = @$"
            INSERT INTO {TableName}
                (name, location, address, capacity, reservered, tariff, daytariff, created_at)
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

    public async Task<ParkingLotModel?> GetParkingLotByID(int id)
    {
        var parameters = new Dictionary<string, object> { { "@ID", id } };
        string query = @$"
            SELECT * FROM {TableName} WHERE id = @ID";
        await using var reader = await Connection.ExecuteQuery(query, parameters);
        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

    public async Task<ParkingLotModel?> GetParkingLotByAddress(string address)
    {
        var parameters = new Dictionary<string, object> { { "@Address", address } };
        string query = @$"
            SELECT * FROM {TableName} WHERE address = @Address";
        await using var reader = await Connection.ExecuteQuery(query, parameters);
        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

    public async Task<ParkingLotModel?> UpdateParkingLotByID(ParkingLotModel parkingLot, int id)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@Name", parkingLot.Name },
            { "@Location", parkingLot.Location },
            { "@Address", parkingLot.Address },
            { "@Capacity", parkingLot.Capacity },
            { "@Reserved", parkingLot.Reserved },
            { "@Tariff", parkingLot.Tariff },
            { "@DayTariff", parkingLot.DayTariff },
            { "@Id", id }
        };
        string query = $@"UPDATE {TableName} SET Name = @Name, Location = @Location, Address = @Address, Capacity = @Capacity,
            Reserved = @Reserved, Tariff = @Tariff, DayTariff = @DayTariff OUTPUT INSERTED.Id WHERE Id = @Id;";
        var result = await Connection.ExecuteScalar(query, parameters);

        if(result is not null)
            return await GetParkingLotByID(Convert.ToInt32(result));
        return null;
    }
    
    public async Task<ParkingLotModel?> UpdateParkingLotByAddress(ParkingLotModel parkingLot, string address)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@Name", parkingLot.Name },
            { "@Location", parkingLot.Location },
            { "@Address", parkingLot.Address },
            { "@Capacity", parkingLot.Capacity },
            { "@Reserved", parkingLot.Reserved },
            { "@Tariff", parkingLot.Tariff },
            { "@DayTariff", parkingLot.DayTariff },
            { "@OldAddress", address }
        };
        string query = $@"UPDATE {TableName} SET Name = @Name, Location = @Location, Address = @Address, Capacity = @Capacity,
            Reserved = @Reserved, Tariff = @Tariff, DayTariff = @DayTariff OUTPUT INSERTED.Id WHERE Address = @OldAddress;";
        var result = await Connection.ExecuteScalar(query, parameters);

        if(result is not null)
            return await GetParkingLotByID(Convert.ToInt32(result));
        return null;
    }

    public async Task<bool> DeleteParkingLotByID(int id)
    {
        var parameters = new Dictionary<string, object> { { "@Id", id } };
        string query = $"DELETE FROM {TableName} WHERE id = @Id";
        
        var affected = await Connection.ExecuteNonQuery(query, parameters);
        return affected > 0;
    }
    
    public async Task<bool> DeleteParkingLotByAddress(string address)
    {
        var parameters = new Dictionary<string, object> { { "@Address", address } };
        string query = $"DELETE FROM {TableName} WHERE address = @Address";
        
        var affected = await Connection.ExecuteNonQuery(query, parameters);
        return affected > 0;
    }
}
