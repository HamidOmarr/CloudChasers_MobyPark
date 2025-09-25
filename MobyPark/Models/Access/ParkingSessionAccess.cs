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
            { "@ParkingLotId", session.ParkingLotId },
            { "@LicensePlate", session.LicensePlate },
            { "@Started", session.Started }, // ISO 8601
            { "@Stopped", session.Stopped ?? (object)null },
            { "@User", session.User },
            { "@DurationMinutes", session.DurationMinutes },
            { "@Cost", session.Cost },
            { "@PaymentStatus", session.PaymentStatus }
        };

        if (session.Id.HasValue)
            parameters.Add("@id", session.Id);

        return parameters;
    }

    public ParkingSessionAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<List<ParkingSessionModel>> GetByParkingLotId(int parkingLotId)
    {
        var parameters = new Dictionary<string, object> { { "@ParkingLotId", parkingLotId } };
        List<ParkingSessionModel> sessions = [];
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE parking_lot_id = @ParkingLotId", parameters);

        while (await reader.ReadAsync())
            sessions.Add(MapFromReader(reader));

        return sessions;
    }

    public async Task<List<ParkingSessionModel>> GetByUser(string user)
    {
        var parameters = new Dictionary<string, object> { { "@User", user } };
        List<ParkingSessionModel> sessions = [];
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE user_name = @User", parameters);

        while (await reader.ReadAsync())
            sessions.Add(MapFromReader(reader));

        return sessions;
    }

    public async Task<List<ParkingSessionModel>> GetByPaymentStatus(string paymentStatus)
    {
        var parameters = new Dictionary<string, object> { { "@PaymentStatus", paymentStatus } };
        List<ParkingSessionModel> sessions = [];
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE payment_status = @PaymentStatus", parameters);

        while (await reader.ReadAsync())
            sessions.Add(MapFromReader(reader));

        return sessions;
    }
}
