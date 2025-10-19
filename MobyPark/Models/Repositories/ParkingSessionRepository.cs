using Microsoft.EntityFrameworkCore;
using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class ParkingSessionRepository : Repository<ParkingSessionModel>, IParkingSessionRepository
{
    public ParkingSessionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<ParkingSessionModel>> GetByParkingLotId(long parkingLotId) =>
        await DbSet.Where(session => session.ParkingLotId == parkingLotId).ToListAsync();

    public async Task<List<ParkingSessionModel>> GetByLicensePlateNumber(string licensePlateNumber) =>
        await DbSet.Where(session => session.LicensePlateNumber == licensePlateNumber).ToListAsync();

    public async Task<List<ParkingSessionModel>> GetByPaymentStatus(ParkingSessionStatus paymentStatus) =>
        await DbSet.Where(session => session.PaymentStatus == paymentStatus).ToListAsync();

    public async Task<List<ParkingSessionModel>> GetActiveSessions() =>
        await DbSet.Where(session => session.Stopped == null).ToListAsync();

    public async Task<ParkingSessionModel?> GetActiveSessionByLicensePlate(string licensePlateNumber) =>
        await DbSet.FirstOrDefaultAsync(session =>
            session.LicensePlateNumber == licensePlateNumber && session.Stopped == null);

    public async Task<List<ParkingSessionModel>> GetAllRecentSessionsByLicensePlate(string licensePlateNumber, TimeSpan recentDuration)
    {
        var cutoffTime = DateTime.UtcNow - recentDuration;

        return await DbSet
            .Where(session => session.LicensePlateNumber == licensePlateNumber &&
                              session.Started >= cutoffTime)
            .ToListAsync();
    }
}
