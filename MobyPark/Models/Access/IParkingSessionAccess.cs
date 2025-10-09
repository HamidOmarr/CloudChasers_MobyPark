using MobyPark.Services;

namespace MobyPark.Models.Access;

public interface IParkingSessionAccess : IRepository<ParkingSessionModel>
{
    Task<List<ParkingSessionModel>> GetByParkingLotId(int parkingLotId);
    Task<List<ParkingSessionModel>> GetByUser(string user);
    Task<List<ParkingSessionModel>> GetByPaymentStatus(string paymentStatus);
    Task<ParkingSessionModel?> GetActiveByLicensePlate(string licensePlate);
}
