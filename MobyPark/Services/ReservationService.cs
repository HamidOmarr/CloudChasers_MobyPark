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

    public async Task<ReservationModel> CreateReservation(ReservationModel reservation)
    {
        (bool success, int id) = await _dataAccess.Reservations.CreateWithId(reservation);
        if (!success) throw new Exception("Failed to create reservation");

        reservation.Id = id;
        return reservation;
    }

    public async Task<bool> UpdateReservation(ReservationModel reservation)
    {
        var success = await _dataAccess.Reservations.Update(reservation);
        return success;
    }

    // public async Task<ReservationModel> GetReservationById(int id)
    // {
    //     ReservationModel? reservation = await _dataAccess.Reservations.GetById(id);
    //     if (reservation is null) throw new KeyNotFoundException("Reservation not found");
    //
    //     return reservation;
    // }

    public async Task<ReservationModel?> GetReservationById(int id) => await _dataAccess.Reservations.GetById(id);

    public async Task<List<ReservationModel>> GetAllReservations() => await _dataAccess.Reservations.GetAll();

    public async Task<int> CountReservations() => await _dataAccess.Reservations.Count();

    public async Task<bool> DeleteReservation(int id)
    {
        bool success = await _dataAccess.Reservations.Delete(id);
        return success;
    }
}