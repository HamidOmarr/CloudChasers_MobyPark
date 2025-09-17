using Microsoft.Data.Sqlite;
using MobyPark.Services.DatabaseConnection;

namespace MobyPark.Models.Access;

public class ReservationAccess : Repository<ReservationModel>, IReservationAccess
{
    protected override string TableName => "Reservations";
    protected override ReservationModel MapFromReader(SqliteDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(ReservationModel reservation)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@UserId", reservation.UserId },
            { "@ParkingLotId", reservation.ParkingLotId },
            { "@VehicleId", reservation.VehicleId },
            { "@StartTime", reservation.StartTime.ToString("o") }, // ISO 8601
            { "@EndTime", reservation.EndTime.ToString("o") },
            { "@Status", reservation.Status },
            { "@CreatedAt", reservation.CreatedAt.ToString("o") },
            { "@Cost", reservation.Cost }
        };

        if (reservation.Id.HasValue)
            parameters.Add("@Id", reservation.Id);

        return parameters;
    }

    public ReservationAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<List<ReservationModel>> GetByUserId(int userId)
    {
        Dictionary<string, object> parameters = new()
        {
            { "@UserId", userId }
        };

        List<ReservationModel> reservations = [];

        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE UserId = @UserId", parameters);

        while (await reader.ReadAsync())
            reservations.Add(MapFromReader(reader));

        return reservations;
    }

    public async Task<List<ReservationModel>> GetByParkingLotId(int parkingLotId)
    {
        Dictionary<string, object> parameters = new() { { "@ParkingLotId", parkingLotId } };
        List<ReservationModel> reservations = [];
        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE ParkingLotId = @ParkingLotId", parameters);

        while (await reader.ReadAsync())
            reservations.Add(MapFromReader(reader));

        return reservations;
    }

    public async Task<List<ReservationModel>> GetByVehicleId(int vehicleId)
    {
        Dictionary<string, object> parameters = new() { { "@VehicleId", vehicleId } };
        List<ReservationModel> reservations = [];
        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE VehicleId = @VehicleId", parameters);

        while (await reader.ReadAsync())
            reservations.Add(MapFromReader(reader));

        return reservations;
    }

    public async Task<List<ReservationModel>> GetByStatus(string status)
    {
        Dictionary<string, object> parameters = new() { { "@Status", status } };
        List<ReservationModel> reservations = [];
        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE Status = @Status", parameters);

        while (await reader.ReadAsync())
            reservations.Add(MapFromReader(reader));

        return reservations;
    }
}
