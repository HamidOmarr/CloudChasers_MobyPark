namespace MobyPark.Models.Repositories.Interfaces;

public interface IParkingSessionRepository : IRepository<ParkingSessionModel>
{
    Task<List<ParkingSessionModel>> GetByParkingLotId(long parkingLotId);
    Task<List<ParkingSessionModel>> GetByLicensePlateNumber(string licensePlateNumber);
    Task<List<ParkingSessionModel>> GetByPaymentStatus(ParkingSessionStatus paymentStatus);
    Task<List<ParkingSessionModel>> GetActiveSessions();
    Task<ParkingSessionModel?> GetActiveSessionByLicensePlate(string licensePlateNumber);
    Task<List<ParkingSessionModel>> GetAllRecentSessionsByLicensePlate(string licensePlateNumber, TimeSpan recentDuration);
}
