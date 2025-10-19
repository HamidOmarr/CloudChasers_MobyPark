using Microsoft.EntityFrameworkCore;
using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class ReservationRepository : Repository<ReservationModel>, IReservationRepository
{
    public ReservationRepository(AppDbContext context) : base(context) { }

    public async Task<List<ReservationModel>> GetByParkingLotId(long parkingLotId)
    {
        return await DbSet
            .Where(r => r.ParkingLotId == parkingLotId)
            .Include(r => r.ParkingLot)
            .ToListAsync();
    }

    public async Task<List<ReservationModel>> GetByLicensePlate(string licensePlate)
    {
        return await DbSet
            .Where(reservation => reservation.LicensePlate.LicensePlateNumber == licensePlate)
            .Include(reservation => reservation.LicensePlate)
            .Include(reservation => reservation.ParkingLot)
            .ToListAsync();
    }

    public async Task<List<ReservationModel>> GetByStatus(ReservationStatus status)
    {
        return await DbSet
            .Where(reservation => reservation.Status == status)
            .Include(reservation => reservation.ParkingLot)
            .ToListAsync();
    }

    public async Task<List<ReservationModel>> GetActiveReservations()
    {
        return await DbSet
            .Where(reservation =>
                reservation.StartTime <= DateTime.UtcNow
                && reservation.EndTime >= DateTime.UtcNow
                && reservation.Status != ReservationStatus.Cancelled
                && reservation.Status != ReservationStatus.NoShow
                )
            .Include(reservation => reservation.ParkingLot)
            .ToListAsync();
    }
}
