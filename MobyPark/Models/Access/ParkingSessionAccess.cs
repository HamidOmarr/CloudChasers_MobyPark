using Microsoft.Data.Sqlite;
using MobyPark.Services.DatabaseConnection;

namespace MobyPark.Models.Access;

public class ParkingSessionAccess : Repository<ParkingSessionModel>, IParkingSessionAccess
{
    protected override string TableName => "ParkingSessions";
    protected override ParkingSessionModel MapFromReader(SqliteDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(ParkingSessionModel session)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@ParkingLotId", session.ParkingLotId },
            { "@LicensePlate", session.LicensePlate },
            { "@Started", session.Started.ToString("o") }, // ISO 8601
            { "@Stopped", session.Stopped?.ToString("o") ?? (object)DBNull.Value },
            { "@User", session.User },
            { "@DurationMinutes", session.DurationMinutes },
            { "@Cost", session.Cost },
            { "@PaymentStatus", session.PaymentStatus }
        };

        if (session.Id.HasValue)
            parameters.Add("@Id", session.Id);

        return parameters;
    }

    public ParkingSessionAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<List<ParkingSessionModel>> GetByParkingLotId(int parkingLotId)
    {
        var parameters = new Dictionary<string, object> { { "@ParkingLotId", parkingLotId } };
        List<ParkingSessionModel> sessions = [];
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE ParkingLotId = @ParkingLotId", parameters);

        while (await reader.ReadAsync())
            sessions.Add(MapFromReader(reader));

        return sessions;
    }

    public async Task<List<ParkingSessionModel>> GetByUser(string user)
    {
        var parameters = new Dictionary<string, object> { { "@User", user } };
        List<ParkingSessionModel> sessions = [];
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE User = @User", parameters);

        while (await reader.ReadAsync())
            sessions.Add(MapFromReader(reader));

        return sessions;
    }

    public async Task<List<ParkingSessionModel>> GetByPaymentStatus(string paymentStatus)
    {
        var parameters = new Dictionary<string, object> { { "@PaymentStatus", paymentStatus } };
        List<ParkingSessionModel> sessions = [];
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE PaymentStatus = @PaymentStatus", parameters);

        while (await reader.ReadAsync())
            sessions.Add(MapFromReader(reader));

        return sessions;
    }
}
