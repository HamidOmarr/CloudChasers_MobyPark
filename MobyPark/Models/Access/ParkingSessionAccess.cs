using Npgsql;
using MobyPark.Models.Access.DatabaseConnection;

namespace MobyPark.Models.Access;

public class ParkingSessionAccess : Repository<ParkingSessionModel>, IParkingSessionAccess
{
    protected override string TableName => "sessions";
    protected override ParkingSessionModel MapFromReader(NpgsqlDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(ParkingSessionModel session)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@id", session.Id },
            { "@parking_lot_id", session.ParkingLotId },
            { "@license_plate", session.LicensePlate },
            { "@started", session.Started },
            { "@stopped", session.Stopped.HasValue ? session.Stopped.Value : DBNull.Value },
            { "@user_username", session.User },
            { "@duration_minutes", session.DurationMinutes },
            { "@cost", session.Cost },
            { "@payment_status", session.PaymentStatus }
        };

        return parameters;
    }

    public ParkingSessionAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<List<ParkingSessionModel>> GetByParkingLotId(int parkingLotId)
    {
        var parameters = new Dictionary<string, object> { { "@parking_lot_id", parkingLotId } };
        List<ParkingSessionModel> sessions = [];
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE parking_lot_id = @parking_lot_id", parameters);

        while (await reader.ReadAsync())
            sessions.Add(MapFromReader(reader));

        return sessions;
    }

    public async Task<List<ParkingSessionModel>> GetByUser(string user)
    {
        var parameters = new Dictionary<string, object> { { "@user", user } };
        List<ParkingSessionModel> sessions = [];
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE user_username = @user", parameters);

        while (await reader.ReadAsync())
            sessions.Add(MapFromReader(reader));

        return sessions;
    }

    public async Task<List<ParkingSessionModel>> GetByPaymentStatus(string paymentStatus)
    {
        var parameters = new Dictionary<string, object> { { "@payment_status", paymentStatus } };
        List<ParkingSessionModel> sessions = [];
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE payment_status = @payment_status", parameters);

        while (await reader.ReadAsync())
            sessions.Add(MapFromReader(reader));

        return sessions;
    }
}
