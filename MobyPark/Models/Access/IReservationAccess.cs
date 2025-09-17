using MobyPark.Services;

namespace MobyPark.Models.Access;

public interface IReservationAccess : IRepository<ReservationModel>
{
    Task<List<ReservationModel>> GetByUserId(int userId);
    Task<List<ReservationModel>> GetByParkingLotId(int parkingLotId);
    Task<List<ReservationModel>> GetByVehicleId(int vehicleId);
    Task<List<ReservationModel>> GetByStatus(string status);
}
