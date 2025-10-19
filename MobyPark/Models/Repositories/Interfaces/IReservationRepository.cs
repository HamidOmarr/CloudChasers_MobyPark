namespace MobyPark.Models.Repositories.Interfaces;

public interface IReservationRepository : IRepository<ReservationModel>
{
    Task<List<ReservationModel>> GetByParkingLotId(long parkingLotId);
    Task<List<ReservationModel>> GetByLicensePlate(string licensePlate);
    Task<List<ReservationModel>> GetByStatus(ReservationStatus status);
    Task<List<ReservationModel>> GetActiveReservations();
}
