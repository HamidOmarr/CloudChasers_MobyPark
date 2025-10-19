using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Services.Services;

namespace MobyPark.Services;

public class ReservationService
{
    private readonly IReservationRepository _reservations;

    public ReservationService(IRepositoryStack repoStack)
    {
        _reservations = repoStack.Reservations;
    }

    public async Task<ReservationModel> CreateReservation(ReservationModel reservation)
    {
        Validator.Reservation(reservation);

        (bool createdSuccessfully, long id) = await _reservations.CreateWithId(reservation);
        if (!createdSuccessfully) throw new Exception("Failed to create reservation");

        reservation.Id = id;
        return reservation;
    }

    public async Task<ReservationModel> GetReservationById(long id)
    {
        var reservation = await _reservations.GetById<ReservationModel>(id);
        return reservation ?? throw new KeyNotFoundException("Reservation not found");
    }

    public async Task<List<ReservationModel>> GetReservationsByParkingLotId(long parkingLotId)
    {
        var reservation = await _reservations.GetByParkingLotId(parkingLotId);
        return reservation ?? throw new KeyNotFoundException("Reservation not found");
    }

    public async Task<List<ReservationModel>> GetReservationsByLicensePlate(string licensePlate)
    {
        var reservations = await _reservations.GetByLicensePlate(licensePlate);
        if (reservations.Count == 0)
            throw new KeyNotFoundException("No reservations found for the given license plate");
        return reservations;
    }

    public async Task<List<ReservationModel>> GetReservationsByStatus(string status)
    {
        if (!Enum.TryParse<ReservationStatus>(status, true, out var parsedStatus))
            throw new ArgumentException("Invalid reservation status");

        var reservations = await _reservations.GetByStatus(parsedStatus);
        if (reservations.Count == 0)
            throw new KeyNotFoundException("No reservations found for the given status");
        return reservations;
    }

    public async Task<List<ReservationModel>> GetAllReservations()
    {
        var reservations = await _reservations.GetAll();
        if (reservations.Count == 0)
            throw new KeyNotFoundException("No reservations found");
        return reservations;
    }

    public async Task<int> CountReservations() => await _reservations.Count();

    public async Task<bool> UpdateReservation(ReservationModel reservation)
    {
        Validator.Reservation(reservation);

        var updatedSuccessfully = await _reservations.Update(reservation);
        return updatedSuccessfully;
    }

    public async Task<bool> DeleteReservation(long id)
    {
        var reservation = GetReservationById(id);
        bool deletedSuccessfully = await _reservations.Delete(reservation.Result);
        return deletedSuccessfully;
    }
}
