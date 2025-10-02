using Npgsql;
using MobyPark.Models.Access.DatabaseConnection;

namespace MobyPark.Models.Access;

public class ReservationAccess : Repository<ReservationModel>, IReservationAccess
{
    protected override string TableName => "reservations";
    protected override ReservationModel MapFromReader(NpgsqlDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(ReservationModel reservation)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@user_id", reservation.UserId },
            { "@parking_lot_id", reservation.ParkingLotId },
            { "@vehicle_id", reservation.VehicleId },
            { "@start_time", reservation.StartTime },
            { "@end_time", reservation.EndTime },
            { "@status", reservation.Status },
            { "@created_at", reservation.CreatedAt },
            { "@cost", reservation.Cost }
        };

        if (reservation.Id.HasValue)
            parameters.Add("@id", reservation.Id.Value);

        return parameters;
    }

    public ReservationAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<List<ReservationModel>> GetByUserId(int userId)
    {
        Dictionary<string, object> parameters = new()
        {
            { "@user_id", userId }
        };

        List<ReservationModel> reservations = [];

        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE user_id = @user_id", parameters);

        while (await reader.ReadAsync())
            reservations.Add(MapFromReader(reader));

        return reservations;
    }

    public async Task<List<ReservationModel>> GetByParkingLotId(int parkingLotId)
    {
        Dictionary<string, object> parameters = new() { { "@parking_lot_id", parkingLotId } };
        List<ReservationModel> reservations = [];
        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE parking_lot_id = @parking_lot_id", parameters);

        while (await reader.ReadAsync())
            reservations.Add(MapFromReader(reader));

        return reservations;
    }

    public async Task<List<ReservationModel>> GetByVehicleId(int vehicleId)
    {
        Dictionary<string, object> parameters = new() { { "@vehicle_id", vehicleId } };
        List<ReservationModel> reservations = [];
        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE vehicle_id = @vehicle_id", parameters);

        while (await reader.ReadAsync())
            reservations.Add(MapFromReader(reader));

        return reservations;
    }

    public async Task<List<ReservationModel>> GetByStatus(string status)
    {
        Dictionary<string, object> parameters = new() { { "@status", status } };
        List<ReservationModel> reservations = [];
        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE status = @status", parameters);

        while (await reader.ReadAsync())
            reservations.Add(MapFromReader(reader));

        return reservations;
    }
}
