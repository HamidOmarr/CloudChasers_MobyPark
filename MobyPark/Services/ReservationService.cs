using MobyPark.Models;
using MobyPark.Models.DataService;

namespace MobyPark.Services;

public class ReservationService
{
    private readonly IDataAccess _dataAccess;

    public ReservationService(IDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public async Task<int> CreateReservation(ReservationModel reservation)
    {
        (bool success, int id) = await _dataAccess.Reservations.CreateWithId(reservation);
        if (success) return id;
        throw new Exception("Failed to create reservation");
    }

    public async Task<ReservationModel> UpdateReservation(int parkingLotId, int vehicleId, DateTime startTime,
        DateTime endTime, int userId)
    {
        var reservation = new ReservationModel
        {
            ParkingLotId = parkingLotId,
            VehicleId = vehicleId,
            StartTime = startTime,
            EndTime = endTime,
            UserId = userId
        };

        await _dataAccess.Reservations.Update(reservation);
        return reservation;
    }

    public async Task<ReservationModel> GetReservationById(int id)
    {
        ReservationModel? reservation = await _dataAccess.Reservations.GetById(id);
        if (reservation is null) throw new KeyNotFoundException("Reservation not found");

        return reservation;
    }

    public async Task<bool> DeleteReservation(int id)
    {
        bool success = await _dataAccess.Reservations.Delete(id);
        return success;
    }
}